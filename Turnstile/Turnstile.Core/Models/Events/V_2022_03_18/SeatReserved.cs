// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Turnstile.Core.Constants;

namespace Turnstile.Core.Models.Events.V_2022_03_18
{
    public class SeatReserved : BaseSeatEvent
    {
        public SeatReserved()
            : base(EventTypes.SeatReserved) { }

        public SeatReserved(Subscription subscription, Seat seat, SeatingSummary seatSummary)
            : base(EventTypes.SeatReserved, subscription, seat)
        {
            ArgumentNullException.ThrowIfNull(seatSummary);

            SeatingSummary = seatSummary;
        }

        [JsonPropertyName("subscription_seats")]
        public SeatingSummary? SeatingSummary { get; set; }
    }
}
