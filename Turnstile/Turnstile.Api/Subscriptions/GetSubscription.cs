// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Threading.Tasks;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;

namespace Turnstile.Api.Subscriptions
{
    public static class GetSubscription
    {
        [FunctionName("GetSubscription")]
        [OpenApiOperation("getSubscription", "subscriptions")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiParameter("subscriptionId", Required = true, In = ParameterLocation.Path)]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, "text/plain", typeof(string))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Subscription))]
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
