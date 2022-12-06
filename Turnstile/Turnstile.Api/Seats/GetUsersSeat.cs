// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Turnstile.Core.Interfaces;

namespace Turnstile.Api.Seats
{
    public static class GetUsersSeat
    {
        [FunctionName("GetUsersSeat")]
        public static async Task<IActionResult> RunGetUsersSeat(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "saas/subscriptions/{subscriptionId}/user-seat/{tenantId}/{userId}")] HttpRequest req,
            ITurnstileRepository turnstileRepo, ILogger log, string subscriptionId, string tenantId, string userId)
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
