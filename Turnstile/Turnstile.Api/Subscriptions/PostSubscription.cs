// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Messaging.EventGrid;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Turnstile.Api.Extensions;
using Turnstile.Core.Constants;
using Turnstile.Core.Extensions;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Configuration;
using Turnstile.Core.Models.Events.V_2022_03_18;
using static Turnstile.Core.Constants.EnvironmentVariableNames;

namespace Turnstile.Api.Subscriptions;

public class PostSubscription
{
    private readonly ITurnstileRepository turnstileRepo;

    public PostSubscription(ITurnstileRepository turnstileRepo) => this.turnstileRepo = turnstileRepo;

    [FunctionName("PostSubscription")]
    [OpenApiOperation("postSubscription", "subscriptions")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
    [OpenApiParameter("subscriptionId", Required = true, In = ParameterLocation.Path)]
    [OpenApiRequestBody("application/json", typeof(Subscription))]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "text/plain", typeof(string))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Subscription))]
    public async Task<IActionResult> RunPostSubscription(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "saas/subscriptions/{subscriptionId}")] HttpRequest req,
        [Blob("turn-configuration/publisher_config.json", FileAccess.Read, Connection = Storage.StorageConnectionString)] string publisherConfigJson,
        [EventGrid(TopicEndpointUri = EventGrid.EndpointUrl, TopicKeySetting = EventGrid.AccessKey)] IAsyncCollector<EventGridEvent> eventCollector,
        string subscriptionId)
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

        await turnstileRepo.CreateSubscription(subscription);
        await eventCollector.AddAsync(new SubscriptionCreated(subscription).ToEventGridEvent());

        return new OkObjectResult(subscription);
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
