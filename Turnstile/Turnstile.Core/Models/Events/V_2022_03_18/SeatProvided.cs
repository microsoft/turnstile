// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Turnstile.Core.Constants;

namespace Turnstile.Core.Models.Events.V_2022_03_18;

public class SeatProvided : BaseSeatEvent
{
    public SeatProvided()
        : base(EventTypes.SeatProvided) { }

    public SeatProvided(Subscription subscription, Seat seat, SeatingSummary seatSummary)
        : base(EventTypes.SeatProvided, subscription, seat)
    {
        ArgumentNullException.ThrowIfNull(seatSummary, nameof(seatSummary));

        SeatingSummary = seatSummary;
    }

    [JsonPropertyName("subscription_seats")]
    public SeatingSummary? SeatingSummary { get; set; }
}
