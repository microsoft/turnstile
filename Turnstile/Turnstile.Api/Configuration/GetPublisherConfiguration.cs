// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using System.IO;
using System.Net;
using System.Text.Json;
using Turnstile.Core.Models.Configuration;
using static Turnstile.Core.Constants.EnvironmentVariableNames;

namespace Turnstile.Api.Configuration;

public static class GetPublisherConfiguration
{
	[FunctionName("GetPublisherConfiguration")]
	[OpenApiOperation("getPublisherConfiguration", "publisherConfiguration")]
	[OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
	[OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(PublisherConfiguration))]
	public static IActionResult RunGetPublisherConfiguration(
		[HttpTrigger(AuthorizationLevel.Function, "get", Route = "saas/publisher/configuration")] HttpRequest req,
		[Blob("turn-configuration/publisher_config.json", FileAccess.Read, Connection = Storage.StorageConnectionString)] string publisherConfigJson) =>
		new OkObjectResult(JsonSerializer.Deserialize<PublisherConfiguration>(publisherConfigJson));
}
