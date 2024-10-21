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
    public class RequestSeat
    {
        private readonly IApiAuthorizationService authService;
        private readonly ILogger log;
        private readonly ISubscriptionEventPublisher eventPublisher;
        private readonly ITurnstileRepository turnstileRepo;

        public RequestSeat(
            IApiAuthorizationService authService,
            ILogger<RequestSeat> log,
            ISubscriptionEventPublisher eventPublisher,
            ITurnstileRepository turnstileRepo)
        {
            this.authService = authService;
            this.log = log;
            this.eventPublisher = eventPublisher;
            this.turnstileRepo = turnstileRepo;
        }

        [Function("RequestSeat")]
        public async Task<IActionResult> RunRequestSeat(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "saas/subscriptions/{subscriptionId}/seats/{seatId}/request")] HttpRequest req,
            string seatId, string subscriptionId)
        {
            if (await authService.IsAuthorized(req))
            {
                var httpContent = await new StreamReader(req.Body).ReadToEndAsync();

                if (string.IsNullOrEmpty(httpContent))
                {
                    return new BadRequestObjectResult("User is required.");
                }

                var user = JsonSerializer.Deserialize<User>(httpContent);
                var subscription = await turnstileRepo.GetSubscription(subscriptionId);

                if (subscription == null)
                {
                    return new NotFoundObjectResult($"Subscription [{subscriptionId}] not found.");
                }

                var validationErrors = user.ValidateSeatRequest(subscription);

                if (validationErrors.Any())
                {
                    return new BadRequestObjectResult(validationErrors.ToParagraph());
                }

                var seat = await turnstileRepo.GetSeat(seatId, subscriptionId);

                if (seat != null)
                {
                    return new ConflictObjectResult($"Seat [{seatId}] already exists.");
                }

                user.UserName ??= user.Email;

                seat = new Seat
                {
                    CreationDateTimeUtc = DateTime.UtcNow,
                    ExpirationDateTimeUtc = subscription.SeatingConfiguration.CalculateSeatExpirationDate(),
                    Occupant = user,
                    SeatId = seatId,
                    SeatingStrategyName = subscription.SeatingConfiguration.SeatingStrategyName,
                    SeatType = SeatTypes.Standard,
                    SubscriptionId = subscriptionId
                };

                var seatCreationResult = await turnstileRepo.CreateSeat(seat, subscription);

                await eventPublisher.PublishSeatWarningEvents(subscription, seatCreationResult.SeatingSummary);

                if (seatCreationResult.IsSeatCreated)
                {
                    log.LogInformation(
                        $"Seat [{seatId}] successfully provided in subscription [{subscriptionId}] to user [{user.UserId}]. " +
                        $"This seat expires at [{seat.ExpirationDateTimeUtc}].");

                    await eventPublisher.PublishEvent(
                        new SeatProvided(subscription, seat, seatCreationResult.SeatingSummary));

                    return new OkObjectResult(seat);
                }
                else if (subscription.SeatingConfiguration.LimitedOverflowSeatingEnabled == true)
                {
                    // Try it again without a total seats # to create a limited seat...

                    seat.ExpirationDateTimeUtc = DateTime.UtcNow.Date.AddDays(1); // Limited seats only last for one day.
                    seat.SeatType = SeatTypes.Limited;

                    seatCreationResult = await turnstileRepo.CreateSeat(seat, subscription);

                    if (seatCreationResult.IsSeatCreated)
                    {
                        log.LogInformation(
                            $"Limited seat [{seatId}] successfully provided in subscription [{subscriptionId}] to user [{user.UserId}]. " +
                            $"This seat expires at [{seat.ExpirationDateTimeUtc}].");

                        await eventPublisher.PublishEvent(
                            new SeatProvided(subscription, seat, seatCreationResult.SeatingSummary));

                        return new OkObjectResult(seat); // Just OK :D
                    }
                }

                // If we've gotten to this point, we weren't able to get get a seat in the subscription...

                log.LogInformation($"Could not provide seat in subscription [{subscriptionId}]. No more seats available.");

                return new NotFoundObjectResult($"No seats available in subscription [{subscriptionId}].");
            }
            else
            {
                return new ForbidResult();
            }
        }
    }
}
