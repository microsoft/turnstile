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
using Turnstile.Core.Models.Events.V_2022_03_18;
using Turnstile.Services.Cosmos;
using static Turnstile.Core.Constants.EnvironmentVariableNames;

namespace Turnstile.Api.Seats
{
    public static class ReleaseSeat
    {
        [FunctionName("ReleaseSeat")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "saas/subscriptions/{subscriptionId}/seats/{seatId}")] HttpRequest req,
            [EventGrid(TopicEndpointUri = EventGrid.EndpointUrl, TopicKeySetting = EventGrid.AccessKey)] IAsyncCollector<EventGridEvent> eventCollector,
            ILogger log, string subscriptionId, string seatId)
        {
            var repo = new CosmosTurnstileRepository(CosmosConfiguration.FromEnvironmentVariables());
            var subscription = await repo.GetSubscription(subscriptionId);
            var seat = await repo.GetSeat(seatId, subscriptionId);

            if (subscription != null && seat != null)
            {
                await repo.DeleteSeat(seatId, subscriptionId);
                await eventCollector.AddAsync(new SeatReleased(subscription, seat).ToEventGridEvent());
            }

            return new NoContentResult();
        }
    }
}
