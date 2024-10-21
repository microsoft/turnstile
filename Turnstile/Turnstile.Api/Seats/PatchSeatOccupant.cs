using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using System.Text.Json;
using Turnstile.Api.Interfaces;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;

namespace Turnstile.Api.Seats
{
    public class PatchSeatOccupant
    {
        private readonly IApiAuthorizationService authService;
        private readonly ITurnstileRepository turnstileRepo;

        public PatchSeatOccupant(
            IApiAuthorizationService authService, 
            ITurnstileRepository turnstileRepo)
        {
            this.authService = authService;
            this.turnstileRepo = turnstileRepo;
        }

        [Function("PatchSeatOccupant")]
        public async Task<IActionResult> RunPatchSeatOccupant(
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "saas/subscriptions/{subscriptionId}/seats/{seatId}")] HttpRequest req,
            string subscriptionId, string seatId)
        {
            if (await authService.IsAuthorized(req))
            {
                var httpContent = await new StreamReader(req.Body).ReadToEndAsync();

                if (string.IsNullOrEmpty(httpContent))
                {
                    return new BadRequestObjectResult("Seat patch is required.");
                }

                var user = JsonSerializer.Deserialize<User>(httpContent);

                if (string.IsNullOrEmpty(user.UserId) || string.IsNullOrEmpty(user.TenantId))
                {
                    return new BadRequestObjectResult("[tenant_id] and [user_id] are required.");
                }

                var subscription = await turnstileRepo.GetSubscription(subscriptionId);

                if (subscription == null)
                {
                    return new NotFoundObjectResult($"Subscription [{subscriptionId}] not found.");
                }

                var seat = await turnstileRepo.GetSeat(seatId, subscriptionId);

                if (seat == null)
                {
                    return new NotFoundObjectResult($"Seat [{seatId}] not found.");
                }

                if (seat.Occupant?.UserId != user.UserId || seat.Occupant?.TenantId != user.TenantId)
                {
                    return new BadRequestObjectResult($"Seat [{seatId}] is not currently occupied by user [{user.TenantId}/{user.UserId}].");
                }

                if (user.Email != null)
                {
                    seat.Occupant.Email = user.Email;
                }

                if (user.UserName != null)
                {
                    seat.Occupant.UserName = user.UserName;
                }

                seat = await turnstileRepo.ReplaceSeat(seat);

                return new OkObjectResult(seat);
            }
            else
            {
                return new ForbidResult();
            }
        }
    }
}
