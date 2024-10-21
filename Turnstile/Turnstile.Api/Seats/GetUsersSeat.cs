using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Turnstile.Api.Interfaces;
using Turnstile.Core.Interfaces;

namespace Turnstile.Api.Seats
{
    public class GetUsersSeat
    {
        private readonly IApiAuthorizationService authService;
        private readonly ITurnstileRepository turnstileRepo;

        public GetUsersSeat(
            IApiAuthorizationService authService, 
            ITurnstileRepository turnstileRepo)
        {
            this.authService = authService;
            this.turnstileRepo = turnstileRepo;
        }

        [Function("GetUsersSeat")]
        public async Task<IActionResult> RunGetUsersSeat(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "saas/subscriptions/{subscriptionId}/user-seat/{tenantId}/{userId}")] HttpRequest req,
            string subscriptionId, string tenantId, string userId)
        {
            if (await authService.IsAuthorized(req))
            {
                var userSeats = (await turnstileRepo.GetSeats(subscriptionId, byUserId: userId)).ToList();

                var userSeat = userSeats.FirstOrDefault(s =>
                    string.Equals(s.Occupant?.UserId, userId, StringComparison.InvariantCultureIgnoreCase) &&
                    string.Equals(s.Occupant?.TenantId, tenantId, StringComparison.InvariantCultureIgnoreCase));

                if (userSeat == null)
                {
                    return new NotFoundObjectResult($"No seat found for user [{tenantId}/{userId}] in subscription [{subscriptionId}].");
                }
                else
                {
                    return new OkObjectResult(userSeat);
                }
            }
            else
            {
                return new ForbidResult();
            }
        }
    }
}
