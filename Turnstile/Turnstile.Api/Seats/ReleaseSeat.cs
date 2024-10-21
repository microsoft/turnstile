using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Turnstile.Api.Interfaces;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models.Events.V_2022_03_18;

namespace Turnstile.Api.Seats
{
    public class ReleaseSeat
    {
        private readonly IApiAuthorizationService authService;
        private readonly ISubscriptionEventPublisher eventPublisher;
        private readonly ITurnstileRepository turnstileRepo;

        public ReleaseSeat(
            IApiAuthorizationService authService,
            ISubscriptionEventPublisher eventPublisher,
            ITurnstileRepository turnstileRepo)
        {
            this.authService = authService;
            this.eventPublisher = eventPublisher;
            this.turnstileRepo = turnstileRepo;
        }

        [Function("ReleaseSeat")]
        public async Task<IActionResult> RunReleaseSeat(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "saas/subscriptions/{subscriptionId}/seats/{seatId}")] HttpRequest req,
            string subscriptionId, string seatId)
        {
            if (await authService.IsAuthorized(req))
            {
                var subscription = await turnstileRepo.GetSubscription(subscriptionId);

                if (subscription != null)
                {
                    var seat = await turnstileRepo.GetSeat(seatId, subscriptionId);

                    if (seat != null)
                    {
                        await turnstileRepo.DeleteSeat(seatId, subscriptionId);
                        await eventPublisher.PublishEvent(new SeatReleased(subscription, seat));
                    }
                }

                return new NoContentResult();
            }
            else
            {
                return new ForbidResult();
            }
        }
    }
}
