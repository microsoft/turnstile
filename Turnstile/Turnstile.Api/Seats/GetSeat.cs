using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Turnstile.Api.Interfaces;
using Turnstile.Core.Interfaces;

namespace Turnstile.Api.Seats
{
    public class GetSeat
    {
        private readonly IApiAuthorizationService authService;
        private readonly ITurnstileRepository turnstileRepo;

        public GetSeat(
            IApiAuthorizationService authService, 
            ITurnstileRepository turnstileRepo)
        {
            this.authService = authService;
            this.turnstileRepo = turnstileRepo;
        }

        [Function("GetSeat")]
        public async Task<IActionResult> RunGetSeat(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "saas/subscriptions/{subscriptionId}/seats/{seatId}")] HttpRequest req,
            string subscriptionId, string seatId)
        {
            if (await authService.IsAuthorized(req))
            {
                var seat = await turnstileRepo.GetSeat(seatId, subscriptionId);

                if (seat == null)
                {
                    return new NotFoundObjectResult($"Seat [{seatId}] not found.");
                }

                return new OkObjectResult(seat);
            }
            else
            {
                return new ForbidResult();
            }
        }
    }
}
