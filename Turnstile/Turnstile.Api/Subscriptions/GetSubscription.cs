// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Threading.Tasks;
using Turnstile.Api.Interfaces;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;

namespace Turnstile.Api.Subscriptions
{
    public class GetSubscription
    {
        private readonly IApiAuthorizationService authService;
        private readonly ITurnstileRepository turnstileRepo;

        public GetSubscription(
            IApiAuthorizationService authService,
            ITurnstileRepository turnstileRepo)
        {
            this.authService = authService;
            this.turnstileRepo = turnstileRepo;
        }

        [Function("GetSubscription")]
        public async Task<IActionResult> RunGetSubscription(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "saas/subscriptions/{subscriptionId}")] HttpRequest req,
            string subscriptionId)
        {
            if (await authService.IsAuthorized(req))
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
            else
            {
                return new ForbidResult();
            }
        }
    }
}
