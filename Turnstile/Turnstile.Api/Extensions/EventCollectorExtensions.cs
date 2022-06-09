// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Messaging.EventGrid;
using Microsoft.Azure.WebJobs;
using System;
using System.Threading.Tasks;
using Turnstile.Core.Extensions;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Events.V_2022_03_18;

namespace Turnstile.Api.Extensions
{
    public static class EventCollectorExtensions
    {
        public async static Task PublishSeatWarningEvents(this IAsyncCollector<EventGridEvent> eventCollector, Subscription subscription, SeatingSummary seatingSummary)
        {
            ArgumentNullException.ThrowIfNull(eventCollector, nameof(eventCollector));
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));
            ArgumentNullException.ThrowIfNull(seatingSummary, nameof(seatingSummary));

            if (subscription.TotalSeats != null)
            {
                if (seatingSummary.StandardSeatCount >= subscription.TotalSeats)
                {
                    await eventCollector.AddAsync(new NoSeatsAvailable(subscription, seatingSummary).ToEventGridEvent());
                }
                else if (subscription.HasReachedLowSeatWarningLevel(seatingSummary))
                {
                    await eventCollector.AddAsync(new LowSeatWarningLevelReached(subscription, seatingSummary).ToEventGridEvent());
                }
            }
        }
    }
}
