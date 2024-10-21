// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Turnstile.Core.Models.Events.V_2022_03_18
{
    public class BaseSeatEvent : BaseSubscriptionEvent
    {
        protected BaseSeatEvent(string eventType)
            : base(eventType) { }

        protected BaseSeatEvent(string eventType, Subscription subscription, Seat seat)
            : base(eventType, subscription)
        {
            ArgumentNullException.ThrowIfNull(seat, nameof(seat));

            Seat = seat;
        }

        [JsonPropertyName("seat")]
        public Seat? Seat { get; set; }
    }
}
