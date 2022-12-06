﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;

namespace Turnstile.Api.Seats
{
    public static class PatchSeatOccupant
    {
        [FunctionName("PatchSeatOccupant")]
        public static async Task<IActionResult> RunPatchSeatOccupant(
            [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "saas/subscriptions/{subscriptionId}/seats/{seatId}")] HttpRequest req,
            ITurnstileRepository turnstileRepo, ILogger log, string subscriptionId, string seatId)
        {
            var httpContent = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrEmpty(httpContent))
            {
                return new BadRequestObjectResult("Seat patch is required.");
            }

            var user = JsonSerializer.Deserialize<User>(httpContent);

            if (string.IsNullOrEmpty(user.UserId) || string.IsNullOrEmpty(user.TenantId))
            {
                return new BadRequestObjectResult("[tenant_id] and [user_id] are required.");
            }

            var subscription = await turnstileRepo.GetSubscription(subscriptionId);

            if (subscription == null)
            {
                return new NotFoundObjectResult($"Subscription [{subscriptionId}] not found.");
            }

            var seat = await turnstileRepo.GetSeat(seatId, subscriptionId);

            if (seat == null)
            {
                return new NotFoundObjectResult($"Seat [{seatId}] not found.");
            }

            if (seat.Occupant?.UserId != user.UserId || seat.Occupant?.TenantId != user.TenantId)
            {
                return new BadRequestObjectResult($"Seat [{seatId}] is not currently occupied by user [{user.TenantId}/{user.UserId}].");
            }

            if (user.Email != null)
            {
                seat.Occupant.Email = user.Email;
            }

            if (user.UserName != null)
            {
                seat.Occupant.UserName = user.UserName;
            }

            seat = await turnstileRepo.ReplaceSeat(seat);

            return new OkObjectResult(seat);
        }
    }
}
