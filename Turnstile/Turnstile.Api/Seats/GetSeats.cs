// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;

namespace Turnstile.Api.Seats;

public class GetSeats
{
    private readonly ITurnstileRepository turnstileRepo;

    public GetSeats(ITurnstileRepository turnstileRepo) => this.turnstileRepo = turnstileRepo;

    [FunctionName("GetSeats")]
    [OpenApiOperation("getSeats", "seats")]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
    [OpenApiParameter("subscriptionId", Required = true, In = ParameterLocation.Path)]
    [OpenApiParameter("user-id", Required = false, In = ParameterLocation.Query)]
    [OpenApiParameter("user-email", Required = false, In = ParameterLocation.Query)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Seat[]))]
    public async Task<IActionResult> RunGetSeats(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "saas/subscriptions/{subscriptionId}/seats")] HttpRequest req,
        ILogger log, string subscriptionId)
    {
        // Originally, these query string parameters were underscore-spaced by default but, after reviewing
        // some web best practices content, I decided to default to "fish-bone" style. There may be customers relying
        // on the underscore-spaced query string parameters so we'll keep backward support here as a fallback if needed.

        var userId = req.Query["user-id"].FirstOrDefault() ?? req.Query["user_id"].FirstOrDefault();
        var userEmail = req.Query["user-email"].FirstOrDefault() ?? req.Query["user_email"].FirstOrDefault();

        var seats = await turnstileRepo.GetSeats(subscriptionId, userId, userEmail);

        return new OkObjectResult(seats);
    }
}
