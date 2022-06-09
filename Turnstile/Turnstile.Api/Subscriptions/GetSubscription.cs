// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Turnstile.Services.Cosmos;

namespace Turnstile.Api.Subscriptions
{
    public static class GetSubscription
    {
        [FunctionName("GetSubscription")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "saas/subscriptions/{subscriptionId}")] HttpRequest req,
            string subscriptionId, ILogger log)
        {
            var repo = new CosmosTurnstileRepository(CosmosConfiguration.FromEnvironmentVariables());
            var subscription = await repo.GetSubscription(subscriptionId);

            if (subscription == null)
            {
                return new NotFoundObjectResult($"Subscription [{subscriptionId}] not found.");
            }
            else
            {
                return new OkObjectResult(subscription);
            }
        }
    }
}
