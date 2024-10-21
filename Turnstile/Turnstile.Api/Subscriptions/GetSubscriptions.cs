// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Turnstile.Api.Interfaces;
using Turnstile.Core.Interfaces;

namespace SMM.API.Subscriptions
{
    public class GetSubscriptions
    {
        private readonly IApiAuthorizationService authService;
        private readonly ITurnstileRepository turnstileRepo;

        public GetSubscriptions(
            IApiAuthorizationService authService,
            ITurnstileRepository turnstileRepo)
        {
            this.authService = authService;
            this.turnstileRepo = turnstileRepo;
        }

        [Function("GetSubscriptions")]
        public async Task<IActionResult> RunGetSubscriptions(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "saas/subscriptions")] HttpRequest req)
        {
            if (await authService.IsAuthorized(req))
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
            else
            {
                return new ForbidResult();
            }
        }
    }
}
