using Azure.Messaging.EventGrid;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Turnstile.Api.Extensions;
using Turnstile.Core.Constants;
using Turnstile.Core.Extensions;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Configuration;
using Turnstile.Core.Models.Events.V_2022_03_18;
using Turnstile.Services.Cosmos;
using static Turnstile.Core.Constants.EnvironmentVariableNames;

namespace Turnstile.Api.Seats
{
    public static class RequestSeat
    {
        [FunctionName("RequestSeat")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "saas/subscriptions/{subscriptionId}/seats/{seatId}/request")] HttpRequest req,
            [EventGrid(TopicEndpointUri = EventGrid.EndpointUrl, TopicKeySetting = EventGrid.AccessKey)] IAsyncCollector<EventGridEvent> eventCollector,
            ILogger log, string seatId, string subscriptionId)
        {
            var httpContent = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrEmpty(httpContent))
            {
                return new BadRequestObjectResult("User is required.");
            }

            var user = JsonSerializer.Deserialize<User>(httpContent);
            var repo = new CosmosTurnstileRepository(CosmosConfiguration.FromEnvironmentVariables());
            var subscription = await repo.GetSubscription(subscriptionId);

            if (subscription == null)
            {
                return new NotFoundObjectResult($"Subscription [{subscriptionId}] not found.");
            }

            var validationErrors = user.ValidateSeatRequest(subscription);

            if (validationErrors.Any())
            {
                return new BadRequestObjectResult(validationErrors.ToParagraph());
            }

            var seat = await repo.GetSeat(seatId, subscriptionId);

            if (seat != null)
            {
                return new ConflictObjectResult($"Seat [{seatId}] already exists.");
            }

            user.UserName ??= user.Email;

            seat = new Seat
            {
                CreationDateTimeUtc = DateTime.UtcNow,
                ExpirationDateTimeUtc = CalculateNewSeatExpirationDate(subscription.SeatingConfiguration),
                Occupant = user,
                SeatId = seatId,
                SeatingStrategyName = subscription.SeatingConfiguration.SeatingStrategyName,
                SeatType = SeatTypes.Standard,
                SubscriptionId = subscriptionId
            };

            var seatCreationResult = await repo.CreateSeat(seat, subscription);

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

                seatCreationResult = await repo.CreateSeat(seat, subscription);

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

        private static DateTime? CalculateNewSeatExpirationDate(SeatingConfiguration seatConfig)
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
