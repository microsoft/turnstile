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
using Turnstile.Core.Extensions;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Events.V_2022_03_18;
using static Turnstile.Core.Constants.EnvironmentVariableNames;

namespace SMM.API.Subscriptions;

public class PatchSubscription
{
    private readonly ITurnstileRepository turnstileRepo;

    public PatchSubscription(ITurnstileRepository turnstileRepo) => this.turnstileRepo = turnstileRepo;

    [FunctionName("PatchSubscription")]
    [OpenApiOperation("patchSubscription", "subscriptions")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
    [OpenApiParameter("subscriptionId", Required = true, In = ParameterLocation.Path)]
    [OpenApiRequestBody("application/json", typeof(Subscription))]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "text/plain", typeof(string))]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "text/plain", typeof(string))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Subscription))]
    public async Task<IActionResult> RunPatchSubscription(
        [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "saas/subscriptions/{subscriptionId}")] HttpRequest req,
        [EventGrid(TopicEndpointUri = EventGrid.EndpointUrl, TopicKeySetting = EventGrid.AccessKey)] IAsyncCollector<EventGridEvent> eventCollector,
        string subscriptionId)
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
        await eventCollector.AddAsync(new SubscriptionUpdated(existingSub).ToEventGridEvent());

        return new OkObjectResult(existingSub);
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
