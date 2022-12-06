// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Messaging.EventGrid;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Turnstile.Api.Extensions;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models.Events.V_2022_03_18;
using static Turnstile.Core.Constants.EnvironmentVariableNames;

namespace Turnstile.Api.Seats
{
    public static class ReleaseSeat
    {
        [FunctionName("ReleaseSeat")]
        public static async Task<IActionResult> RunReleaseSeat(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "saas/subscriptions/{subscriptionId}/seats/{seatId}")] HttpRequest req,
            [EventGrid(TopicEndpointUri = EventGrid.EndpointUrl, TopicKeySetting = EventGrid.AccessKey)] IAsyncCollector<EventGridEvent> eventCollector,
            ITurnstileRepository turnstileRepo, ILogger log, string subscriptionId, string seatId)
        {
            var subscription = await turnstileRepo.GetSubscription(subscriptionId);

            if (subscription != null)
            {
                var seat = await turnstileRepo.GetSeat(seatId, subscriptionId);

                if (seat != null)
                {
                    await turnstileRepo.DeleteSeat(seatId, subscriptionId);
                    await eventCollector.AddAsync(new SeatReleased(subscription, seat).ToEventGridEvent());
                }
            }

            return new NoContentResult();
        }
    }
}
