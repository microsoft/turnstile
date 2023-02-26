// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Turnstile.Core.Constants;

namespace Turnstile.Core.Models.Events.V_2022_03_18;

public class NoSeatsAvailable : BaseSubscriptionEvent
{
    public NoSeatsAvailable()
        : base(EventTypes.NoSeatsAvailable) { }

    public NoSeatsAvailable(Subscription subscription, SeatingSummary seatSummary)
        : base(EventTypes.NoSeatsAvailable, subscription)
    {
        ArgumentNullException.ThrowIfNull(seatSummary, nameof(seatSummary));

        SeatingSummary = seatSummary;
    }

    [JsonPropertyName("subscription_seats")]
    public SeatingSummary? SeatingSummary { get; set; }
}
