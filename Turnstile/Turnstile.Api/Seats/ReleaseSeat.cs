// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Messaging.EventGrid;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Threading.Tasks;
using Turnstile.Api.Extensions;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models.Events.V_2022_03_18;
using static Turnstile.Core.Constants.EnvironmentVariableNames;

namespace Turnstile.Api.Seats
{    
    public class ReleaseSeat
    {
        private readonly ILogger log;
        private readonly ITurnstileRepository turnstileRepo;

        public ReleaseSeat(ILogger log, ITurnstileRepository turnstileRepo)
        {
            this.log = log;
            this.turnstileRepo = turnstileRepo;
        }

        [FunctionName("ReleaseSeat")]
        [OpenApiOperation("releaseSeat", "seats")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiParameter("subscriptionId", Required = true, In = ParameterLocation.Path)]
        [OpenApiParameter("seatId", Required = true, In = ParameterLocation.Path)]
        [OpenApiResponseWithoutBody(HttpStatusCode.NoContent)]
        public async Task<IActionResult> RunReleaseSeat(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "saas/subscriptions/{subscriptionId}/seats/{seatId}")] HttpRequest req,
            [EventGrid(TopicEndpointUri = EventGrid.EndpointUrl, TopicKeySetting = EventGrid.AccessKey)] IAsyncCollector<EventGridEvent> eventCollector,
            string subscriptionId, string seatId)
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
