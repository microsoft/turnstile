// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Turnstile.Services.Cosmos;

namespace Turnstile.Api.Seats
{
    public static class GetSeats
    {
        [FunctionName("GetSeats")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "saas/subscriptions/{subscriptionId}/seats")] HttpRequest req,
            ILogger log, string subscriptionId)
        {
            var userId = req.Query["user_id"];
            var userEmail = req.Query["user_email"];

            var repo = new CosmosTurnstileRepository(CosmosConfiguration.FromEnvironmentVariables());
            var seats = await repo.GetSeats(subscriptionId, userId, userEmail);

            return new OkObjectResult(seats);
        }
    }
}
