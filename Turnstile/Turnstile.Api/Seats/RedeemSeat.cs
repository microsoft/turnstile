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
using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Turnstile.Api.Extensions;
using Turnstile.Core.Constants;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Events.V_2022_03_18;
using static Turnstile.Core.Constants.EnvironmentVariableNames;

namespace Turnstile.Api.Seats
{
    public class RedeemSeat
    {
        [FunctionName("RedeemSeat")]
        [OpenApiOperation("redeemSeat", "seats")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "x-functions-key", In = OpenApiSecurityLocationType.Header)]
        [OpenApiParameter("subscriptionId", Required = true, In = ParameterLocation.Path)]
        [OpenApiParameter("seatId", Required = true, In = ParameterLocation.Path)]
        [OpenApiRequestBody("application/json", typeof(User))]
        [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "text/plain", typeof(string))]
        [OpenApiResponseWithBody(HttpStatusCode.NotFound, "text/plain", typeof(string))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(Seat))]
        public async Task<IActionResult> RunRedeemSeat(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "saas/subscriptions/{subscriptionId}/seats/{seatId}/redeem")] HttpRequest req,
            [EventGrid(TopicEndpointUri = EventGrid.EndpointUrl, TopicKeySetting = EventGrid.AccessKey)] IAsyncCollector<EventGridEvent> eventCollector,
            ITurnstileRepository turnstileRepo, ILogger log, string subscriptionId, string seatId)
        {
            var httpContent = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrEmpty(httpContent))
            {
                return new BadRequestObjectResult("User is required.");
            }

            var user = JsonSerializer.Deserialize<User>(httpContent);

            if (string.IsNullOrEmpty(user.UserId) || string.IsNullOrEmpty(user.TenantId) || string.IsNullOrEmpty(user.Email))
            {
                return new BadRequestObjectResult("[email], [user_id], and [tenant_id] are required.");
            }

            user.UserName ??= user.Email;

            var subscription = await turnstileRepo.GetSubscription(subscriptionId);

            if (subscription == null)
            {
                return new NotFoundObjectResult($"Subscription [{subscriptionId}] not found.");
            }

            var seat = await turnstileRepo.GetSeat(seatId, subscriptionId);

            if (IsReservedFor(seat, user))
            {
                seat.Reservation = null;
                seat.Occupant = user;
                seat.ExpirationDateTimeUtc = subscription.SeatingConfiguration.CalculateSeatExpirationDate();
                seat.RedemptionDateTimeUtc = DateTime.UtcNow;
                seat.SeatType = SeatTypes.Standard;

                // What happens if between the time a seat is reserved and the time it's redeemed
                // the subscription's seating strategy (or seating configuration for that matter) is changed? Right now, we honor
                // the subscription's current seating strategy but it's definitely open for discussion...

                seat.SeatingStrategyName = subscription.SeatingConfiguration.SeatingStrategyName;

                seat = await turnstileRepo.ReplaceSeat(seat);

                log.LogInformation(
                    $"Seat [{seatId}] reservation succesfully redeemed in subscription [{subscriptionId}] by user [{user.UserId}]. " +
                    $"This seat expires at [{seat.ExpirationDateTimeUtc}].");

                await eventCollector.AddAsync(new SeatRedeemed(subscription, seat).ToEventGridEvent());

                return new OkObjectResult(seat);
            }
            else
            {
                return new NotFoundObjectResult($"Seat [{seatId}] is not reserved for this user.");
            }
        }

        public bool IsReservedFor(Seat seat, User user) =>
            seat.Reservation != null && (IsReservedForUserId(seat, user) || IsReservedForEmail(seat, user));

        public bool IsReservedForEmail(Seat seat, User user) =>
            !string.IsNullOrEmpty(seat.Reservation!.Email) &&
            string.Compare(seat.Reservation!.Email, user.Email!, true) == 0;

        public bool IsReservedForUserId(Seat seat, User user) =>
            !string.IsNullOrEmpty(seat.Reservation!.UserId) &&
            !string.IsNullOrEmpty(seat.Reservation!.TenantId) &&
            string.Compare(seat.Reservation!.UserId, user.UserId!, true) == 0 &&
            string.Compare(seat.Reservation!.TenantId, user.TenantId!, true) == 0;
    }
}
