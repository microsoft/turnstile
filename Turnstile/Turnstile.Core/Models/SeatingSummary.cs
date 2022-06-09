// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Turnstile.Core.Models
{
    public class SeatingSummary
    {
        [JsonPropertyName("standard_seat_count")]
        [JsonProperty("standard_seat_count")]
        public int StandardSeatCount { get; set; } = 0;

        [JsonPropertyName("limited_seat_count")]
        [JsonProperty("limited_seat_count")]
        public int LimitedSeatCount { get; set; } = 0;
    }
}
