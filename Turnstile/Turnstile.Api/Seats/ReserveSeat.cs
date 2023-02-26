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
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Turnstile.Api.Extensions;
using Turnstile.Core.Constants;
using Turnstile.Core.Extensions;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Events.V_2022_03_18;
using static Turnstile.Core.Constants.EnvironmentVariableNames;

namespace Turnstile.Api.Seats;

public class ReserveSeat
{
    private readonly ITurnstileRepository turnstileRepo;

    public ReserveSeat(ITurnstileRepository turnstileRepo) => this.turnstileRepo = turnstileRepo;

    [FunctionName("ReserveSeat")]
    [OpenApiOperation("reserveSeat", "seats")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
    [OpenApiParameter("subscriptionId", Required = true, In = ParameterLocation.Path)]
    [OpenApiParameter("seatId", Required = true, In = ParameterLocation.Path)]
    [OpenApiRequestBody("application/json", typeof(Reservation))]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "text/plain", typeof(string))]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "text/plain", typeof(string))]
    [OpenApiResponseWithBody(HttpStatusCode.Conflict, "text/plain", typeof(string))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Seat))]
    public async Task<IActionResult> RunReserveSeat(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "saas/subscriptions/{subscriptionId}/seats/{seatId}/reserve")] HttpRequest req,
        [EventGrid(TopicEndpointUri = EventGrid.EndpointUrl, TopicKeySetting = EventGrid.AccessKey)] IAsyncCollector<EventGridEvent> eventCollector,
        ILogger log, string subscriptionId, string seatId)
    {
        var httpContent = await new StreamReader(req.Body).ReadToEndAsync();

        if (string.IsNullOrEmpty(httpContent))
        {
            return new BadRequestObjectResult("Reservation is required.");
        }

        var reservation = JsonSerializer.Deserialize<Reservation>(httpContent);
        var subscription = await turnstileRepo.GetSubscription(subscriptionId);

        if (subscription == null)
        {
            return new NotFoundObjectResult($"Subscription [{subscriptionId}] not found.");
        }

        var validationErrors = reservation.Validate(subscription);

        if (validationErrors.Any())
        {
            return new BadRequestObjectResult(validationErrors.ToParagraph());
        }

        var seat = await turnstileRepo.GetSeat(seatId, subscriptionId);

        if (seat != null)
        {
            return new ConflictObjectResult($"Seat [{seatId}] already exists.");
        }

        seat = new Seat
        {
            ExpirationDateTimeUtc = subscription.SeatingConfiguration.CalculateSeatReservationExpirationDate(),
            CreationDateTimeUtc = DateTime.UtcNow,
            SubscriptionId = subscriptionId,
            Reservation = reservation,
            SeatId = seatId,
            SeatingStrategyName = subscription.SeatingConfiguration.SeatingStrategyName,
            SeatType = SeatTypes.Standard
        };

        var seatCreationResult = await turnstileRepo.CreateSeat(seat, subscription);

        await eventCollector.PublishSeatWarningEvents(subscription, seatCreationResult.SeatingSummary);

        if (seatCreationResult.IsSeatCreated)
        {
            log.LogInformation(
                $"Seat [{seatId}] successfully reserved in subscription [{subscriptionId}]. " +
                $"This reservation expires at [{seat.ExpirationDateTimeUtc}].");

            await eventCollector.AddAsync(new SeatReserved(subscription, seat, seatCreationResult.SeatingSummary).ToEventGridEvent());

            return new OkObjectResult(seat);
        }
        else
        {
            log.LogWarning($"Can't reserve seat [{seatId}] in subscription [{subscriptionId}]. No more seats available.");

            return new NotFoundObjectResult($"No seats available to reserve in subscription [{subscriptionId}].");
        }
    }
}
