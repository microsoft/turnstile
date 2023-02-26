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
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Turnstile.Core.Extensions;
using Turnstile.Core.Models.Configuration;
using static Turnstile.Core.Constants.EnvironmentVariableNames;

namespace Turnstile.Api.Configuration;

public static class PutPublisherConfiguration
{
	[FunctionName("PutPublisherConfiguration")]
	[OpenApiOperation("putPublisherConfiguration", "publisherConfiguration")]
	[OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
	[OpenApiRequestBody("application/json", typeof(PublisherConfiguration))]
	[OpenApiResponseWithBody(HttpStatusCode.BadRequest, "text/plain", typeof(string))]
	[OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(PublisherConfiguration))]
	public static async Task<IActionResult> Run(
		[HttpTrigger(AuthorizationLevel.Function, "put", Route = "saas/publisher/configuration")] HttpRequest req,
		[Blob("turn-configuration/publisher_config.json", FileAccess.Write, Connection = Storage.StorageConnectionString)] Stream pubConfigStream)
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

		await JsonSerializer.SerializeAsync(pubConfigStream, pubConfig);

		return new OkObjectResult(pubConfig);
	}
}
