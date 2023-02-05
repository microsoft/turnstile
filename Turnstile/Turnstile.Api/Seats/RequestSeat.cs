// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Messaging.EventGrid;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Turnstile.Api.Extensions;
using Turnstile.Core.Constants;
using Turnstile.Core.Extensions;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Events.V_2022_03_18;
using static Turnstile.Core.Constants.EnvironmentVariableNames;

namespace Turnstile.Api.Seats
{
    public class RequestSeat
    {
        [FunctionName("RequestSeat")]
        [OpenApiOperation("requestSeat", "seats")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiParameter("subscriptionId", Required = true, In = ParameterLocation.Path)]
        [OpenApiParameter("seatId", Required = true, In = ParameterLocation.Path)]
        [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "text/plain", typeof(string))]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, "text/plain", typeof(string))]
        [OpenApiResponseWithBody(HttpStatusCode.Conflict, "text/plain", typeof(string))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Seat))]
        public async Task<IActionResult> RunRequestSeat(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "saas/subscriptions/{subscriptionId}/seats/{seatId}/request")] HttpRequest req,
            [EventGrid(TopicEndpointUri = EventGrid.EndpointUrl, TopicKeySetting = EventGrid.AccessKey)] IAsyncCollector<EventGridEvent> eventCollector,
            ITurnstileRepository turnstileRepo, ILogger log, string seatId, string subscriptionId)
        {
            var httpContent = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrEmpty(httpContent))
            {
                return new BadRequestObjectResult("User is required.");
            }

            var user = JsonSerializer.Deserialize<User>(httpContent);
            var subscription = await turnstileRepo.GetSubscription(subscriptionId);

            if (subscription == null)
            {
                return new NotFoundObjectResult($"Subscription [{subscriptionId}] not found.");
            }

            var validationErrors = user.ValidateSeatRequest(subscription);

            if (validationErrors.Any())
            {
                return new BadRequestObjectResult(validationErrors.ToParagraph());
            }

            var seat = await turnstileRepo.GetSeat(seatId, subscriptionId);

            if (seat != null)
            {
                return new ConflictObjectResult($"Seat [{seatId}] already exists.");
            }

            user.UserName ??= user.Email;

            seat = new Seat
            {
                CreationDateTimeUtc = DateTime.UtcNow,
                ExpirationDateTimeUtc = subscription.SeatingConfiguration.CalculateSeatExpirationDate(),
                Occupant = user,
                SeatId = seatId,
                SeatingStrategyName = subscription.SeatingConfiguration.SeatingStrategyName,
                SeatType = SeatTypes.Standard,
                SubscriptionId = subscriptionId
            };

            var seatCreationResult = await turnstileRepo.CreateSeat(seat, subscription);

            await eventCollector.PublishSeatWarningEvents(subscription, seatCreationResult.SeatingSummary);

            if (seatCreationResult.IsSeatCreated)
            {
                log.LogInformation(
                    $"Seat [{seatId}] successfully provided in subscription [{subscriptionId}] to user [{user.UserId}]. " +
                    $"This seat expires at [{seat.ExpirationDateTimeUtc}].");

                await eventCollector.AddAsync(new SeatProvided(subscription, seat, seatCreationResult.SeatingSummary).ToEventGridEvent());

                return new OkObjectResult(seat);
            }
            else if (subscription.SeatingConfiguration.LimitedOverflowSeatingEnabled == true)
            {
                // Try it again without a total seats # to create a limited seat...

                seat.ExpirationDateTimeUtc = DateTime.UtcNow.Date.AddDays(1); // Limited seats only last for one day.
                seat.SeatType = SeatTypes.Limited;

                seatCreationResult = await turnstileRepo.CreateSeat(seat, subscription);

                if (seatCreationResult.IsSeatCreated)
                {
                    log.LogInformation(
                        $"Limited seat [{seatId}] successfully provided in subscription [{subscriptionId}] to user [{user.UserId}]. " +
                        $"This seat expires at [{seat.ExpirationDateTimeUtc}].");

                    await eventCollector.AddAsync(new SeatProvided(subscription, seat, seatCreationResult.SeatingSummary).ToEventGridEvent());

                    return new OkObjectResult(seat); // Just OK :D
                }
            }

            // If we've gotten to this point, we weren't able to get get a seat in the subscription...

            log.LogInformation($"Could not provide seat in subscription [{subscriptionId}]. No more seats available.");

            return new NotFoundObjectResult($"No seats available in subscription [{subscriptionId}].");
        }
    }
}
