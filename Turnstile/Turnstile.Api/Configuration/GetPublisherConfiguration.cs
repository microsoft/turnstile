using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Turnstile.Api.Interfaces;
using Turnstile.Core.Interfaces;

namespace Turnstile.Api.Configuration
{
    public class GetPublisherConfiguration
    {
        private readonly IApiAuthorizationService authService;
        private readonly IPublisherConfigurationStore publisherConfigStore;

        public GetPublisherConfiguration(
            IApiAuthorizationService authService,
            IPublisherConfigurationStore publisherConfigStore)
        {
            this.authService = authService;
            this.publisherConfigStore = publisherConfigStore;
        }

        [Function("GetPublisherConfiguration")]
        public async Task<IActionResult> RunGetPublisherConfiguration(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "saas/publisher/configuration")] HttpRequest req)
        {
            if (await authService.IsAuthorized(req))
            {
                var publisherConfig = await publisherConfigStore.GetConfiguration();

                if (publisherConfig == null)
                {
                    return new NotFoundResult();
                }
                else
                {
                    return new OkObjectResult(publisherConfig);
                }
            }
            else
            {
                return new ForbidResult();
            }
        }
    }
}
