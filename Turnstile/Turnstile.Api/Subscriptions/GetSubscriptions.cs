// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Turnstile.Services.Cosmos;

namespace SMM.API.Subscriptions
{
    public static class GetSubscriptions
    {
        [FunctionName("GetSubscriptions")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "saas/subscriptions")] HttpRequest req,
            ILogger log)
        {
            var offerId = req.Query["offer_id"];
            var planId = req.Query["plan_id"];
            var state = req.Query["state"];
            var tenantId = req.Query["tenant_id"];

            var repo = new CosmosTurnstileRepository(CosmosConfiguration.FromEnvironmentVariables());
            var subscriptions = await repo.GetSubscriptions(state, offerId, planId, tenantId);

            return new OkObjectResult(subscriptions);
        }
    }
}
