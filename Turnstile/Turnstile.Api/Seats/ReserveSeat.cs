// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Messaging.EventGrid;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Turnstile.Api.Extensions;
using Turnstile.Core.Constants;
using Turnstile.Core.Extensions;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Events.V_2022_03_18;
using Turnstile.Services.Cosmos;
using static Turnstile.Core.Constants.EnvironmentVariableNames;

namespace Turnstile.Api.Seats
{
    public static class ReserveSeat
    {
        [FunctionName("ReserveSeat")]
        public static async Task<IActionResult> Run(
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
            var repo = new CosmosTurnstileRepository(CosmosConfiguration.FromEnvironmentVariables());
            var subscription = await repo.GetSubscription(subscriptionId);

            if (subscription == null)
            {
                return new NotFoundObjectResult($"Subscription [{subscriptionId}] not found.");
            }

            var validationErrors = reservation.Validate(subscription);

            if (validationErrors.Any())
            {
                return new BadRequestObjectResult(validationErrors.ToParagraph());
            }

            var seat = await repo.GetSeat(seatId, subscriptionId);

            if (seat != null)
            {
                return new ConflictObjectResult($"Seat [{seatId}] already exists.");
            }

            seat = new Seat
            {
                ExpirationDateTimeUtc = DateTime.UtcNow.Date.AddDays((double)subscription.SeatingConfiguration.SeatReservationExpiryInDays),
                CreationDateTimeUtc = DateTime.UtcNow,
                SubscriptionId = subscriptionId,
                Reservation = reservation,
                SeatId = seatId,
                SeatingStrategyName = subscription.SeatingConfiguration.SeatingStrategyName,
                SeatType = SeatTypes.Standard
            };

            var seatCreationResult = await repo.CreateSeat(seat, subscription);

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
}
