// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Linq;
using System.Threading.Tasks;
using Turnstile.Core.Interfaces;

namespace SMM.API.Subscriptions
{
    public static class GetSubscriptions
    {
        [FunctionName("GetSubscriptions")]
        public static async Task<IActionResult> RunGetSubscriptions(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "saas/subscriptions")] HttpRequest req,
            ITurnstileRepository turnstileRepo)
        {
            // Originally, these query string parameters were underscore-spaced by default but, after reviewing
            // some web best practices content, I decided to default to "fish-bone" style. There may be customers relying
            // on the underscore-spaced query string parameters so we'll keep backward support here as a fallback if needed.

            var state = req.Query["state"].FirstOrDefault();
            var offerId = req.Query["offer-id"].FirstOrDefault() ?? req.Query["offer_id"].FirstOrDefault();
            var planId = req.Query["plan-id"].FirstOrDefault() ?? req.Query["plan_id"].FirstOrDefault();
            var tenantId = req.Query["tenant-id"].FirstOrDefault() ?? req.Query["tenant_id"].FirstOrDefault();

            var subscriptions = await turnstileRepo.GetSubscriptions(state, offerId, planId, tenantId);

            return new OkObjectResult(subscriptions);
        }
    }
}
