// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Threading.Tasks;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;

namespace Turnstile.Api.Seats
{
    public static class GetSeat
    {
        [FunctionName("GetSeat")]
        [OpenApiOperation("getSeat", "seats")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiParameter("subscriptionId", Required = true, In = ParameterLocation.Path)]
        [OpenApiParameter("seatId", Required = true, In = ParameterLocation.Path)]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, "text/plain", typeof(string))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Seat))]
        public static async Task<IActionResult> RunGetSeat(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "saas/subscriptions/{subscriptionId}/seats/{seatId}")] HttpRequest req,
            ITurnstileRepository turnstileRepo, string subscriptionId, string seatId)
        {
            var seat = await turnstileRepo.GetSeat(seatId, subscriptionId);

            if (seat == null)
            {
                return new NotFoundObjectResult($"Seat [{seatId}] not found.");
            }

            return new OkObjectResult(seat);
        }
    }
}
