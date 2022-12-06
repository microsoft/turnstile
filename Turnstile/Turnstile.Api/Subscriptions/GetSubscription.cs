// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Turnstile.Core.Interfaces;

namespace Turnstile.Api.Subscriptions
{
    public static class GetSubscription
    {
        [FunctionName("GetSubscription")]
        public static async Task<IActionResult> RunGetSubscription(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "saas/subscriptions/{subscriptionId}")] HttpRequest req,
            ITurnstileRepository turnstileRepo, string subscriptionId, ILogger log)
        {
            var subscription = await turnstileRepo.GetSubscription(subscriptionId);

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
