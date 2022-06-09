// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Turnstile.Core.Constants;

namespace Turnstile.Core.Models.Events.V_2022_03_18
{
    public class LowSeatWarningLevelReached : BaseSubscriptionEvent
    {
        public LowSeatWarningLevelReached()
            : base(EventTypes.SeatingWarningLeavelReached) { }

        public LowSeatWarningLevelReached(Subscription subscription, SeatingSummary seatSummary)
            : base(EventTypes.SeatingWarningLeavelReached, subscription)
        {
            ArgumentNullException.ThrowIfNull(seatSummary, nameof(seatSummary));

            SeatingSummary = seatSummary;
        }

        [JsonPropertyName("subscription_seats")]
        public SeatingSummary? SeatingSummary { get; set; }
    }
}
