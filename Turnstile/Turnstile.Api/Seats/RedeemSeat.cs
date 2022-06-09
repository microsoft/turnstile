// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Messaging.EventGrid;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Turnstile.Api.Extensions;
using Turnstile.Core.Constants;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Configuration;
using Turnstile.Core.Models.Events.V_2022_03_18;
using Turnstile.Services.Cosmos;
using static Turnstile.Core.Constants.EnvironmentVariableNames;

namespace Turnstile.Api.Seats
{
    public static class RedeemSeat
    {
        [FunctionName("RedeemSeat")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "saas/subscriptions/{subscriptionId}/seats/{seatId}/redeem")] HttpRequest req,
            [EventGrid(TopicEndpointUri = EventGrid.EndpointUrl, TopicKeySetting = EventGrid.AccessKey)] IAsyncCollector<EventGridEvent> eventCollector,
            ILogger log, string subscriptionId, string seatId)
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

            var repo = new CosmosTurnstileRepository(CosmosConfiguration.FromEnvironmentVariables());
            var subscription = await repo.GetSubscription(subscriptionId);

            if (subscription == null)
            {
                return new NotFoundObjectResult($"Subscription [{subscriptionId}] not found.");
            }

            var seat = await repo.GetSeat(seatId, subscriptionId);

            if (seat.IsReservedFor(user))
            {
                seat.Reservation = null;
                seat.Occupant = user;
                seat.ExpirationDateTimeUtc = CalculateRedeemedSeatExpirationDate(subscription.SeatingConfiguration);
                seat.RedemptionDateTimeUtc = DateTime.UtcNow;
                seat.SeatType = SeatTypes.Standard;

                // What happens if between the time a seat is reserved and the time it's redeemed
                // the subscription's seating strategy (or seating configuration for that matter) is changed? Right now, we honor
                // the subscription's current seating strategy but it's definitely open for discussion...

                seat.SeatingStrategyName = subscription.SeatingConfiguration.SeatingStrategyName;

                seat = await repo.ReplaceSeat(seat);

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

        private static bool IsReservedFor(this Seat seat, User user) =>
            seat.Reservation != null && (seat.IsReservedForUserId(user) || seat.IsReservedForEmail(user));

        private static bool IsReservedForEmail(this Seat seat, User user) =>
            !string.IsNullOrEmpty(seat.Reservation!.Email) &&
            string.Compare(seat.Reservation!.Email, user.Email!, true) == 0;

        private static bool IsReservedForUserId(this Seat seat, User user) =>
            !string.IsNullOrEmpty(seat.Reservation!.UserId) &&
            !string.IsNullOrEmpty(seat.Reservation!.TenantId) &&
            string.Compare(seat.Reservation!.UserId, user.UserId!, true) == 0 &&
            string.Compare(seat.Reservation!.TenantId, user.TenantId!, true) == 0;

        // TODO: We do this in the RequestSeat method so we should probably stick this logic somewhere else...

        private static DateTime? CalculateRedeemedSeatExpirationDate(SeatingConfiguration seatConfig)
        {
            var now = DateTime.UtcNow;

            return seatConfig.SeatingStrategyName switch
            {
                SeatingStrategies.MonthlyActiveUser => new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month)).AddDays(1),
                SeatingStrategies.FirstComeFirstServed => now.Date.AddDays((double?)seatConfig.DefaultSeatExpiryInDays ?? 1),
                _ => throw new ArgumentException($"Seating strategy [{seatConfig.SeatingStrategyName}] not supported.")
            };
        }
    }
}
