// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Turnstile.Core.Models
{
    public class SeatCreationContext
    {
        [JsonPropertyName("is_seat_created")]
        [JsonProperty("is_seat_created")]
        public bool IsSeatCreated { get; set; } = false;

        [JsonPropertyName("seating_summary")]
        [JsonProperty("seating_summary")]
        public SeatingSummary? SeatingSummary { get; set; }

        [JsonPropertyName("created_seat")]
        [JsonProperty("created_seat")]
        public Seat? CreatedSeat { get; set; }
    }
}
