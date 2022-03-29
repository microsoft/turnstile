using Azure.Messaging.EventGrid;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Turnstile.Api.Extensions;
using Turnstile.Core.Extensions;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Events.V_2022_03_18;
using Turnstile.Services.Cosmos;
using static Turnstile.Core.Constants.EnvironmentVariableNames;

namespace SMM.API.Subscriptions
{
    public static class PatchSubscription
    {
        [FunctionName("PatchSubscription")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "saas/subscriptions/{subscriptionId}")] HttpRequest req,
            [EventGrid(TopicEndpointUri = EventGrid.EndpointUrl, TopicKeySetting = EventGrid.AccessKey)] IAsyncCollector<EventGridEvent> eventCollector,
            string subscriptionId, ILogger log)
        {
            var httpContent = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrEmpty(httpContent))
            {
                return new BadRequestObjectResult("Subscription patch is required.");
            }

            var patch = JsonConvert.DeserializeObject<Subscription>(httpContent);
            var repo = new CosmosTurnstileRepository(CosmosConfiguration.FromEnvironmentVariables());
            var existingSub = await repo.GetSubscription(subscriptionId);

            if (existingSub == null)
            {
                return new NotFoundObjectResult($"Subscription [{subscriptionId}] not found.");
            }

            var validationErrors = existingSub.ValidatePatch(patch);

            if (validationErrors.Any())
            {
                return new BadRequestObjectResult(validationErrors.ToParagraph());
            }

            ApplyPatch(patch, existingSub);

            await repo.ReplaceSubscription(existingSub);
            await eventCollector.AddAsync(new SubscriptionUpdated(existingSub).ToEventGridEvent());

            return new OkObjectResult(existingSub);
        }

        private static void ApplyPatch(Subscription patch, Subscription existingSub)
        {
            existingSub.PlanId = patch.PlanId ?? existingSub.PlanId;
            existingSub.IsBeingConfigured = patch.IsBeingConfigured ?? existingSub.IsBeingConfigured;
            existingSub.SourceSubscription = patch.SourceSubscription ?? existingSub.SourceSubscription;
            existingSub.SubscriberInfo = patch.SubscriberInfo ?? existingSub.SubscriberInfo;
            existingSub.SubscriptionName = patch.SubscriptionName ?? existingSub.SubscriptionName;
            existingSub.TotalSeats = patch.TotalSeats ?? existingSub.TotalSeats;
            existingSub.AdminRoleName = patch.AdminRoleName ?? existingSub.AdminRoleName;
            existingSub.UserRoleName = patch.UserRoleName ?? existingSub.UserRoleName;
            existingSub.IsSetupComplete = patch.IsSetupComplete ?? existingSub.IsSetupComplete;
            existingSub.ManagementUrls = patch.ManagementUrls ?? existingSub.ManagementUrls;

            ApplySeatingConfigurationPatch(patch, existingSub);

            if (!string.IsNullOrEmpty(patch.State) && existingSub.State != patch.State)
            {
                existingSub.State = patch.State.ToLower();
                existingSub.StateLastUpdatedDateTimeUtc = DateTime.UtcNow;
            }
        }

        private static void ApplySeatingConfigurationPatch(Subscription patch, Subscription existingSub)
        {
            if (patch.SeatingConfiguration != null)
            {
                var existSeatConfig = existingSub.SeatingConfiguration;
                var patchSeatConfig = patch.SeatingConfiguration;

                existSeatConfig.DefaultSeatExpiryInDays =
                    patchSeatConfig?.DefaultSeatExpiryInDays ?? existSeatConfig!.DefaultSeatExpiryInDays;

                existSeatConfig.SeatReservationExpiryInDays =
                    patchSeatConfig?.SeatReservationExpiryInDays ?? existSeatConfig!.SeatReservationExpiryInDays;

                existSeatConfig.LimitedOverflowSeatingEnabled =
                    patchSeatConfig?.LimitedOverflowSeatingEnabled ?? existSeatConfig!.LimitedOverflowSeatingEnabled;

                existSeatConfig.LowSeatWarningLevelPercent =
                    patchSeatConfig?.LowSeatWarningLevelPercent ?? existSeatConfig!.LowSeatWarningLevelPercent;

                existSeatConfig.SeatingStrategyName =
                    patchSeatConfig?.SeatingStrategyName ?? existSeatConfig!.SeatingStrategyName;
            }
        }
    }
}
