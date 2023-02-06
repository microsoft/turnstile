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
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;

namespace Turnstile.Api.Seats
{
    public class GetUsersSeat
    {
        private readonly ITurnstileRepository turnstileRepo;

        public GetUsersSeat(ITurnstileRepository turnstileRepo) => this.turnstileRepo = turnstileRepo;

        [FunctionName("GetUsersSeat")]
        [OpenApiOperation("getUsersSeat", "seat")]
        [OpenApiParameter("subscriptionId", Required = true, In = ParameterLocation.Path)]
        [OpenApiParameter("tenantId", Required = true, In = ParameterLocation.Path)]
        [OpenApiParameter("userId", Required = true, In = ParameterLocation.Path)]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, "text/plain", typeof(string))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Seat))]
        public async Task<IActionResult> RunGetUsersSeat(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "saas/subscriptions/{subscriptionId}/user-seat/{tenantId}/{userId}")] HttpRequest req,
            ILogger log, string subscriptionId, string tenantId, string userId)
        {
            var userSeats = (await turnstileRepo.GetSeats(subscriptionId, byUserId: userId)).ToList();

            var userSeat = userSeats.FirstOrDefault(s =>
                string.Equals(s.Occupant?.UserId, userId, StringComparison.InvariantCultureIgnoreCase) &&
                string.Equals(s.Occupant?.TenantId, tenantId, StringComparison.InvariantCultureIgnoreCase));

            if (userSeat == null)
            {
                return new NotFoundObjectResult($"No seat found for user [{tenantId}/{userId}] in subscription [{subscriptionId}].");
            }
            else
            {
                return new OkObjectResult(userSeat);
            }
        }
    }
}
