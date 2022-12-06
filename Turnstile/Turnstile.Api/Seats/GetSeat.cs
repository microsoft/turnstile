// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Threading.Tasks;
using Turnstile.Core.Interfaces;

namespace Turnstile.Api.Seats
{
    public static class GetSeat
    {
        [FunctionName("GetSeat")]
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
