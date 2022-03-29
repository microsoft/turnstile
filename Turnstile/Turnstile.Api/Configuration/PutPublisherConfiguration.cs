using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using static Turnstile.Core.Constants.EnvironmentVariableNames;

namespace Turnstile.Api.Configuration
{
    public static class PutPublisherConfiguration
    {
        [FunctionName("PutPublisherConfiguration")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "saas/publisher/configuration")] HttpRequest req,
            [Blob("configuration/publisher_config.json", FileAccess.ReadWrite, Connection = Storage.StorageConnectionString)] out string publisherConfigJson,
            ILogger log)
        {
            var httpContent =  new StreamReader(req.Body).ReadToEnd();

            // TODO: Validate publisher configuration model...

            publisherConfigJson = httpContent;

            return new OkResult();
        }
    }
}
