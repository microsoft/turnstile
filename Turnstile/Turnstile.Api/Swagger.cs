using AzureFunctions.Extensions.Swashbuckle;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Turnstile.Api
{
    public static class SwaggerController
    {
        public const string JsonRoute = "swagger/json";
        public const string UiRoute = "swagger/ui";

        [SwaggerIgnore]
        [FunctionName("SwaggerJson")]
        public static Task<HttpResponseMessage> RunGetSwaggerJson(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = JsonRoute)] HttpRequestMessage req,
            [SwashBuckleClient] ISwashBuckleClient sbClient) =>
            Task.FromResult(sbClient.CreateSwaggerDocumentResponse(req));

        [SwaggerIgnore]
        [FunctionName("SwaggerUi")]
        public static Task<HttpResponseMessage> RunGetSwaggerUi(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = UiRoute)] HttpRequestMessage req,
            [SwashBuckleClient] ISwashBuckleClient sbClient) =>
            Task.FromResult(sbClient.CreateSwaggerUIResponse(req, JsonRoute));
    }
}
