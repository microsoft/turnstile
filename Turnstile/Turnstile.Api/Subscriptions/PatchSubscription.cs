// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using Turnstile.Api.Interfaces;
using Turnstile.Core.Extensions;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Events.V_2022_03_18;

namespace SMM.API.Subscriptions
{
    public class PatchSubscription
    {
        private readonly IApiAuthorizationService authService;
        private readonly ISubscriptionEventPublisher eventPublisher;
        private readonly ITurnstileRepository turnstileRepo;

        public PatchSubscription(
            IApiAuthorizationService authService,
            ISubscriptionEventPublisher eventPublisher,
            ITurnstileRepository turnstileRepo)
        {
            this.authService = authService;
            this.eventPublisher = eventPublisher;
            this.turnstileRepo = turnstileRepo;
        }

        [Function("PatchSubscription")]
        public async Task<IActionResult> RunPatchSubscription(
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "saas/subscriptions/{subscriptionId}")] HttpRequest req,
            string subscriptionId)
        {
            if (await authService.IsAuthorized(req))
            {
                var httpContent = await new StreamReader(req.Body).ReadToEndAsync();

                if (string.IsNullOrEmpty(httpContent))
                {
                    return new BadRequestObjectResult("Subscription patch is required.");
                }

                var subPatch = JsonConvert.DeserializeObject<Subscription>(httpContent);
                var existingSub = await turnstileRepo.GetSubscription(subscriptionId);

                if (existingSub == null)
                {
                    return new NotFoundObjectResult($"Subscription [{subscriptionId}] not found.");
                }

                var validationErrors = existingSub.ValidatePatch(subPatch);

                if (validationErrors.Any())
                {
                    return new BadRequestObjectResult(validationErrors.ToParagraph());
                }

                ApplyPatch(subPatch, existingSub);

                await turnstileRepo.ReplaceSubscription(existingSub);
                await eventPublisher.PublishEvent(new SubscriptionUpdated(existingSub));

                return new OkObjectResult(existingSub);
            }
            else
            {
                return new ForbidResult();
            }
        }

        private void ApplyPatch(Subscription patch, Subscription existingSub)
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
            existingSub.AdminName = patch.AdminName ?? existingSub.AdminName;
            existingSub.AdminEmail = patch.AdminEmail ?? existingSub.AdminEmail;
            existingSub.TenantName = patch.TenantName ?? existingSub.TenantName;

            ApplySeatingConfigurationPatch(patch, existingSub);

            if (!string.IsNullOrEmpty(patch.State) && existingSub.State != patch.State)
            {
                existingSub.State = patch.State.ToLower();
                existingSub.StateLastUpdatedDateTimeUtc = DateTime.UtcNow;
            }
        }

        private void ApplySeatingConfigurationPatch(Subscription patch, Subscription existingSub)
        {
            if (patch.SeatingConfiguration != null)
            {
                var existSeatConfig = existingSub.SeatingConfiguration;
                var patchSeatConfig = patch.SeatingConfiguration;

                existSeatConfig.DefaultSeatExpiryInDays =
                    patchSeatConfig?.DefaultSeatExpiryInDays ??
                    existSeatConfig!.DefaultSeatExpiryInDays;

                existSeatConfig.SeatReservationExpiryInDays =
                    patchSeatConfig?.SeatReservationExpiryInDays ??
                    existSeatConfig!.SeatReservationExpiryInDays;

                existSeatConfig.LimitedOverflowSeatingEnabled =
                    patchSeatConfig?.LimitedOverflowSeatingEnabled ??
                    existSeatConfig!.LimitedOverflowSeatingEnabled;

                existSeatConfig.SeatingStrategyName =
                    patchSeatConfig?.SeatingStrategyName ??
                    existSeatConfig!.SeatingStrategyName;
            }
        }
    }
}
