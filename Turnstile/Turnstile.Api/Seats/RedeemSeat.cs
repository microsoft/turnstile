using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Turnstile.Api.Extensions;
using Turnstile.Api.Interfaces;
using Turnstile.Core.Constants;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Events.V_2022_03_18;

namespace Turnstile.Api.Seats
{
    public class RedeemSeat
    {
        private readonly IApiAuthorizationService authService;
        private readonly ILogger log;
        private readonly ISubscriptionEventPublisher eventPublisher;
        private readonly ITurnstileRepository turnstileRepo;

        public RedeemSeat(
            IApiAuthorizationService authService,
            ILogger<RedeemSeat> log,
            ISubscriptionEventPublisher eventPublisher,
            ITurnstileRepository turnstileRepo)
        {
            this.authService = authService;
            this.log = log;
            this.eventPublisher = eventPublisher;
            this.turnstileRepo = turnstileRepo;
        }

        [Function("RedeemSeat")]
        public async Task<IActionResult> RunRedeemSeat(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "saas/subscriptions/{subscriptionId}/seats/{seatId}/redeem")] HttpRequest req,
            string subscriptionId, string seatId)
        {
            if (await authService.IsAuthorized(req))
            {
                var httpContent = await new StreamReader(req.Body).ReadToEndAsync();

                if (string.IsNullOrEmpty(httpContent))
                {
                    return new BadRequestObjectResult("User is required.");
                }

                var user = JsonSerializer.Deserialize<User>(httpContent);

                if (string.IsNullOrEmpty(user.UserId) || string.IsNullOrEmpty(user.TenantId) || string.IsNullOrEmpty(user.Email))
                {
                    return new BadRequestObjectResult("[email], [user_id], and [tenant_id] are required.");
                }

                user.UserName ??= user.Email;

                var subscription = await turnstileRepo.GetSubscription(subscriptionId);

                if (subscription == null)
                {
                    return new NotFoundObjectResult($"Subscription [{subscriptionId}] not found.");
                }

                var seat = await turnstileRepo.GetSeat(seatId, subscriptionId);

                if (IsReservedFor(seat, user))
                {
                    seat.Reservation = null;
                    seat.Occupant = user;
                    seat.ExpirationDateTimeUtc = subscription.SeatingConfiguration.CalculateSeatExpirationDate();
                    seat.RedemptionDateTimeUtc = DateTime.UtcNow;
                    seat.SeatType = SeatTypes.Standard;

                    // What happens if between the time a seat is reserved and the time it's redeemed
                    // the subscription's seating strategy (or seating configuration for that matter) is changed? Right now, we honor
                    // the subscription's current seating strategy but it's definitely open for discussion...

                    seat.SeatingStrategyName = subscription.SeatingConfiguration.SeatingStrategyName;

                    seat = await turnstileRepo.ReplaceSeat(seat);

                    log.LogInformation(
                        $"Seat [{seatId}] reservation succesfully redeemed in subscription [{subscriptionId}] by user [{user.UserId}]. " +
                        $"This seat expires at [{seat.ExpirationDateTimeUtc}].");

                    await eventPublisher.PublishEvent(new SeatRedeemed(subscription, seat));

                    return new OkObjectResult(seat);
                }
                else
                {
                    return new NotFoundObjectResult($"Seat [{seatId}] is not reserved for this user.");
                }
            }
            else
            {
                return new ForbidResult();
            }
        }

        public bool IsReservedFor(Seat seat, User user) =>
            seat.Reservation != null && (IsReservedForUserId(seat, user) || IsReservedForEmail(seat, user));

        public bool IsReservedForEmail(Seat seat, User user) =>
            !string.IsNullOrEmpty(seat.Reservation!.Email) &&
            string.Compare(seat.Reservation!.Email, user.Email!, true) == 0;

        public bool IsReservedForUserId(Seat seat, User user) =>
            !string.IsNullOrEmpty(seat.Reservation!.UserId) &&
            !string.IsNullOrEmpty(seat.Reservation!.TenantId) &&
            string.Compare(seat.Reservation!.UserId, user.UserId!, true) == 0 &&
            string.Compare(seat.Reservation!.TenantId, user.TenantId!, true) == 0;
    }
}
