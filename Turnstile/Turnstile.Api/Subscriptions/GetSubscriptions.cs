// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;

namespace SMM.API.Subscriptions
{
    public class GetSubscriptions
    {
        [FunctionName("GetSubscriptions")]
        [OpenApiOperation("getSubscriptions", "subscriptions")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiParameter("state", Required = false, In = ParameterLocation.Query)]
        [OpenApiParameter("offer-id", Required = false, In = ParameterLocation.Query)]
        [OpenApiParameter("plan-id", Required = false, In = ParameterLocation.Query)]
        [OpenApiParameter("tenant-id", Required = false, In = ParameterLocation.Query)]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Subscription[]))]
        public async Task<IActionResult> RunGetSubscriptions(
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
