// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Turnstile.Api.Interfaces;
using Turnstile.Core.Constants;
using Turnstile.Core.Extensions;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Configuration;
using Turnstile.Core.Models.Events.V_2022_03_18;

namespace Turnstile.Api.Subscriptions
{
    public class PostSubscription
    {
        private readonly IApiAuthorizationService authService;
        private readonly IPublisherConfigurationStore publisherConfigStore;
        private readonly ISubscriptionEventPublisher eventPublisher;
        private readonly ITurnstileRepository turnstileRepo;

        public PostSubscription(
            IApiAuthorizationService authService,
            IPublisherConfigurationStore publisherConfigStore,
            ISubscriptionEventPublisher eventPublisher,
            ITurnstileRepository turnstileRepo)
        {
            this.authService = authService;
            this.eventPublisher = eventPublisher;
            this.publisherConfigStore = publisherConfigStore;
            this.turnstileRepo = turnstileRepo;
        }

        [Function("PostSubscription")]
        public async Task<IActionResult> RunPostSubscription(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "saas/subscriptions/{subscriptionId}")] HttpRequest req,
            string subscriptionId)
        {
            if (await authService.IsAuthorized(req))
            {
                var httpContent = await new StreamReader(req.Body).ReadToEndAsync();

                if (string.IsNullOrEmpty(httpContent))
                {
                    return new BadRequestObjectResult("Subscription is required.");
                }

                var subscription = JsonConvert.DeserializeObject<Subscription>(httpContent);
                var publisherConfig = await publisherConfigStore.GetConfiguration();

                if (publisherConfig?.IsSetupComplete != true)
                {
                    return new StatusCodeResult(StatusCodes.Status503ServiceUnavailable);
                }

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

                await turnstileRepo.CreateSubscription(subscription);
                await eventPublisher.PublishEvent(new SubscriptionCreated(subscription));

                return new OkObjectResult(subscription);
            }
            else
            {
                return new ForbidResult();
            }
        }

        private SeatingConfiguration ConfigureSubscriptionSeating(SeatingConfiguration defaultSeatConfig, Subscription subscription)
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
