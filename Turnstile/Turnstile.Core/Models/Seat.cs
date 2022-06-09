// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System.Text.Json.Serialization;
using Turnstile.Core.Constants;

namespace Turnstile.Core.Models
{
    public class Seat
    {
        [JsonPropertyName("seat_id")]
        [JsonProperty("seat_id")]
        public string? SeatId { get; set; }

        [JsonPropertyName("subscription_id")]
        [JsonProperty("subscription_id")]
        public string? SubscriptionId { get; set; }

        [JsonPropertyName("occupant")]
        [JsonProperty("occupant")]
        public User? Occupant { get; set; }

        [JsonPropertyName("seating_strategy_name")]
        [JsonProperty("seating_strategy_name")]
        public string? SeatingStrategyName { get; set; }

        [JsonPropertyName("seat_type")]
        [JsonProperty("seat_type")]
        public string SeatType { get; set; } = SeatTypes.Standard;

        [JsonPropertyName("reservation")]
        [JsonProperty("reservation")]
        public Reservation? Reservation { get; set; }

        [JsonPropertyName("expires_utc")]
        [JsonProperty("expires_utc")]
        public DateTime? ExpirationDateTimeUtc { get; set; }

        [JsonPropertyName("created_utc")]
        [JsonProperty("created_utc")]
        public DateTime? CreationDateTimeUtc { get; set; }

        [JsonPropertyName("redeemed_utc")]
        [JsonProperty("redeemed_utc")]
        public DateTime? RedemptionDateTimeUtc { get; set; }
    }
}
