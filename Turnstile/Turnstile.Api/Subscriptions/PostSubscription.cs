// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
using Turnstile.Core.Constants;
using Turnstile.Core.Extensions;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Configuration;
using Turnstile.Core.Models.Events.V_2022_03_18;
using Turnstile.Services.Cosmos;
using static Turnstile.Core.Constants.EnvironmentVariableNames;

namespace Turnstile.Api.Subscriptions
{
    public static class PostSubscription
    {
        [FunctionName("PostSubscription")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "saas/subscriptions/{subscriptionId}")] HttpRequest req,
            [Blob("turn-configuration/publisher_config.json", FileAccess.Read, Connection = Storage.StorageConnectionString)] string publisherConfigJson,
            [EventGrid(TopicEndpointUri = EventGrid.EndpointUrl, TopicKeySetting = EventGrid.AccessKey)] IAsyncCollector<EventGridEvent> eventCollector,
            string subscriptionId, ILogger log)
        {
            var httpContent = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrEmpty(httpContent))
            {
                return new BadRequestObjectResult("Subscription is required.");
            }

            var subscription = JsonConvert.DeserializeObject<Subscription>(httpContent);
            var publisherConfig = JsonConvert.DeserializeObject<PublisherConfiguration>(publisherConfigJson);

            subscription.SubscriptionId = subscriptionId;
            subscription.CreatedDateTimeUtc = DateTime.UtcNow;
            subscription.StateLastUpdatedDateTimeUtc = DateTime.UtcNow;
            subscription.SeatingConfiguration = ConfigureSubscriptionSeating(publisherConfig.SeatingConfiguration, subscription);

            subscription.State ??= SubscriptionStates.Purchased;
            subscription.SubscriptionName ??= subscription.SubscriptionId;
            subscription.IsSetupComplete ??= false;

            subscription.State = subscription.State.ToLower();

            var validationErrors = subscription.Validate();

            if (validationErrors.Any())
            {
                return new BadRequestObjectResult(validationErrors.ToParagraph());
            }

            var repo = new CosmosTurnstileRepository(CosmosConfiguration.FromEnvironmentVariables());

            await repo.CreateSubscription(subscription);
            await eventCollector.AddAsync(new SubscriptionCreated(subscription).ToEventGridEvent());

            return new OkObjectResult(subscription);
        }

        private static SeatingConfiguration ConfigureSubscriptionSeating(SeatingConfiguration defaultSeatConfig, Subscription subscription)
        {
            var seatingConfig = subscription.SeatingConfiguration ?? new SeatingConfiguration();

            seatingConfig.SeatingStrategyName ??= defaultSeatConfig.SeatingStrategyName;
            seatingConfig.DefaultSeatExpiryInDays ??= defaultSeatConfig.DefaultSeatExpiryInDays;
            seatingConfig.LimitedOverflowSeatingEnabled ??= defaultSeatConfig.LimitedOverflowSeatingEnabled;
            seatingConfig.SeatReservationExpiryInDays ??= defaultSeatConfig.SeatReservationExpiryInDays;

            return seatingConfig;
        }
    }
}
