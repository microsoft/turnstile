// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Turnstile.Api.Extensions;
using Turnstile.Api.Interfaces;
using Turnstile.Core.Constants;
using Turnstile.Core.Extensions;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Events.V_2022_03_18;

namespace Turnstile.Api.Seats
{
    public class ReserveSeat
    {
        private readonly IApiAuthorizationService authService;
        private readonly ILogger log;
        private readonly ISubscriptionEventPublisher eventPublisher;
        private readonly ITurnstileRepository turnstileRepo;

        public ReserveSeat(
            IApiAuthorizationService authService,
            ILogger<ReserveSeat> log,
            ISubscriptionEventPublisher eventPublisher,
            ITurnstileRepository turnstileRepo)
        {
            this.authService = authService;
            this.log = log;
            this.eventPublisher = eventPublisher;
            this.turnstileRepo = turnstileRepo;
        }

        [Function("ReserveSeat")]
        public async Task<IActionResult> RunReserveSeat(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "saas/subscriptions/{subscriptionId}/seats/{seatId}/reserve")] HttpRequest req,
            string subscriptionId, string seatId)
        {
            if (await authService.IsAuthorized(req))
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

                await eventPublisher.PublishSeatWarningEvents(subscription, seatCreationResult.SeatingSummary);

                if (seatCreationResult.IsSeatCreated)
                {
                    log.LogInformation(
                        $"Seat [{seatId}] successfully reserved in subscription [{subscriptionId}]. " +
                        $"This reservation expires at [{seat.ExpirationDateTimeUtc}].");

                    await eventPublisher.PublishEvent(
                        new SeatReserved(subscription, seat, seatCreationResult.SeatingSummary));

                    return new OkObjectResult(seat);
                }
                else
                {
                    log.LogWarning($"Can't reserve seat [{seatId}] in subscription [{subscriptionId}]. No more seats available.");

                    return new NotFoundObjectResult($"No seats available to reserve in subscription [{subscriptionId}].");
                }
            }
            else
            {
                return new ForbidResult();
            }
        }
    }
}
