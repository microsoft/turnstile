using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Turnstile.Api.Interfaces;
using Turnstile.Core.Interfaces;

namespace Turnstile.Api.Seats
{
    public class GetSeats
    {
        private readonly IApiAuthorizationService authService;
        private readonly ITurnstileRepository turnstileRepo;

        public GetSeats(
            IApiAuthorizationService authService, 
            ITurnstileRepository turnstileRepo)
        {
            this.authService = authService;
            this.turnstileRepo = turnstileRepo;
        }

        [Function("GetSeats")]
        public async Task<IActionResult> RunGetSeats(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "saas/subscriptions/{subscriptionId}/seats")] HttpRequest req,
            string subscriptionId)
        {
            if (await authService.IsAuthorized(req))
            {
                // Originally, these query string parameters were underscore-spaced by default but, after reviewing
                // some web best practices content, I decided to default to "fish-bone" style. There may be customers relying
                // on the underscore-spaced query string parameters so we'll keep backward support here as a fallback if needed.

                var userId = req.Query["user-id"].FirstOrDefault() ?? req.Query["user_id"].FirstOrDefault();
                var userEmail = req.Query["user-email"].FirstOrDefault() ?? req.Query["user_email"].FirstOrDefault();

                var seats = await turnstileRepo.GetSeats(subscriptionId, userId, userEmail);

                return new OkObjectResult(seats);
            }
            else
            {
                return new ForbidResult();
            }
        }
    }
}
