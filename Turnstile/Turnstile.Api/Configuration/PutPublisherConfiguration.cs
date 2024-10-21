using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using System.Text.Json;
using Turnstile.Api.Interfaces;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Extensions;
using Turnstile.Core.Models.Configuration;

namespace Turnstile.Api.Configuration
{
    public class PutPublisherConfiguration
    {
        private readonly IApiAuthorizationService authService;
        private readonly IPublisherConfigurationStore publisherConfigStore;

        public PutPublisherConfiguration(
            IApiAuthorizationService authService,
            IPublisherConfigurationStore publisherConfigStore)
        {
            this.authService = authService;
            this.publisherConfigStore = publisherConfigStore;
        }

        [Function("PutPublisherConfiguration")]
        public async Task<IActionResult> RunPutPublisherConfiguration(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "saas/publisher/configuration")] HttpRequest req)
        {
            if (await authService.IsAuthorized(req))
            {
                var httpContent = await new StreamReader(req.Body).ReadToEndAsync();

                if (string.IsNullOrEmpty(httpContent))
                {
                    return new BadRequestObjectResult("Publisher configuration is required.");
                }

                var pubConfig = JsonSerializer.Deserialize<PublisherConfiguration>(httpContent);

                if (pubConfig.IsSetupComplete == true)
                {
                    var validationErrors = pubConfig.Validate().ToList();

                    if (validationErrors.Any())
                    {
                        return new BadRequestObjectResult($"If [is_setup_complete]... {validationErrors.ToParagraph()}");
                    }
                }

                await publisherConfigStore.PutConfiguration(pubConfig);

                return new OkObjectResult(pubConfig);
            }
            else
            {
                return new ForbidResult();
            }
        }
    }
}
