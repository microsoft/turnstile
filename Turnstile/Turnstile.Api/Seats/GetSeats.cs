// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using Turnstile.Core.Interfaces;

namespace Turnstile.Api.Seats
{
    public static class GetSeats
    {
        [FunctionName("GetSeats")]
        public static async Task<IActionResult> RunGetSeats(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "saas/subscriptions/{subscriptionId}/seats")] HttpRequest req,
            ITurnstileRepository turnstileRepo, ILogger log, string subscriptionId)
        {
            // Originally, these query string parameters were underscore-spaced by default but, after reviewing
            // some web best practices content, I decided to default to "fish-bone" style. There may be customers relying
            // on the underscore-spaced query string parameters so we'll keep backward support here as a fallback if needed.

            var userId = req.Query["user-id"].FirstOrDefault() ?? req.Query["user_id"].FirstOrDefault();
            var userEmail = req.Query["user-email"].FirstOrDefault() ?? req.Query["user_email"].FirstOrDefault();

            var seats = await turnstileRepo.GetSeats(subscriptionId, userId, userEmail);

            return new OkObjectResult(seats);
        }
    }
}
