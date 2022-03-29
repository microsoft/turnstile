using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Turnstile.Services.Cosmos;

namespace Turnstile.Api.Seats
{
    public static class GetSeat
    {
        [FunctionName("GetSeat")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "saas/subscriptions/{subscriptionId}/seats/{seatId}")] HttpRequest req,
            ILogger log, string subscriptionId, string seatId)
        {
            var repo = new CosmosTurnstileRepository(CosmosConfiguration.FromEnvironmentVariables());
            var seat = await repo.GetSeat(seatId, subscriptionId);

            if (seat == null)
            {
                return new NotFoundObjectResult($"Seat [{seatId}] not found.");
            }

            return new OkObjectResult(seat);
        }
    }
}
