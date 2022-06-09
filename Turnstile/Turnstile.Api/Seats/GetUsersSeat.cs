// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using Turnstile.Services.Cosmos;

namespace Turnstile.Api.Seats
{
    public static class GetUsersSeat
    {
        [FunctionName("GetUsersSeat")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "saas/subscriptions/{subscriptionId}/user-seat/{tenantId}/{userId}")] HttpRequest req,
            ILogger log, string subscriptionId, string tenantId, string userId)
        {
            subscriptionId = subscriptionId.ToLower();
            tenantId = tenantId.ToLower();
            userId = userId.ToLower();

            var repo = new CosmosTurnstileRepository(CosmosConfiguration.FromEnvironmentVariables());
            var userSeats = (await repo.GetSeats(subscriptionId, byUserId: userId)).ToList();
            var userSeat = userSeats.FirstOrDefault(s => s.Occupant?.UserId == userId && s.Occupant?.TenantId == tenantId);

            if (userSeat == null)
            {
                return new NotFoundObjectResult($"No seat found for user [{tenantId}/{userId}] in subscription [{subscriptionId}].");
            }
            else
            {
                return new OkObjectResult(userSeat);
            }
        }
    }
}
