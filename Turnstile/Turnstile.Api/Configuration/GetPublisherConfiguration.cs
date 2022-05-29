using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;
using Turnstile.Core.Models.Configuration;
using static Turnstile.Core.Constants.EnvironmentVariableNames;

namespace Turnstile.Api.Configuration
{
    public static class GetPublisherConfiguration
    {
        [FunctionName("GetPublisherConfiguration")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "saas/publisher/configuration")] HttpRequest req,
            [Blob("turn-configuration/publisher_config.json", FileAccess.Read, Connection = Storage.StorageConnectionString)] string publisherConfigJson,
            ILogger log) =>
            new OkObjectResult(JsonSerializer.Deserialize<PublisherConfiguration>(publisherConfigJson));
    }
}
