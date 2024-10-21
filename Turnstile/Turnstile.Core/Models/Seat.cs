// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using Turnstile.Core.Constants;

namespace Turnstile.Core.Models
{
    public class Seat
    {
        [JsonPropertyName("seat_id")]
        [JsonProperty("seat_id")]
        [OpenApiProperty(Nullable = false, Description = "Unique seat identifier")]
        public string? SeatId { get; set; }

        [JsonPropertyName("subscription_id")]
        [JsonProperty("subscription_id")]
        [OpenApiProperty(Nullable = false, Description = "Subscription (ID) that this seat exists in")]
        public string? SubscriptionId { get; set; }

        [JsonPropertyName("occupant")]
        [JsonProperty("occupant")]
        [OpenApiProperty(Nullable = true, Description = "This seat's occupant (if occupied)")]
        public User? Occupant { get; set; }

        [JsonPropertyName("seating_strategy_name")]
        [JsonProperty("seating_strategy_name")]
        [OpenApiProperty(Nullable = true, Description = "The strategy used to provide this seat; options are [first_come_first_served] or [monthly_active_user]")]
        public string? SeatingStrategyName { get; set; }

        [JsonPropertyName("seat_type")]
        [JsonProperty("seat_type")]
        [OpenApiProperty(Nullable = false, Description = "Options include [standard] or [limited]")]
        public string SeatType { get; set; } = SeatTypes.Standard;

        [JsonPropertyName("reservation")]
        [JsonProperty("reservation")]
        [OpenApiProperty(Nullable = true, Description = "The user this seat is reserved for (if reserved)")]
        public Reservation? Reservation { get; set; }

        [JsonPropertyName("expires_utc")]
        [JsonProperty("expires_utc")]
        [OpenApiProperty(Nullable = true, Description = "The date/time (UTC) at which this seat is set to expire")]
        public DateTime? ExpirationDateTimeUtc { get; set; }

        [JsonPropertyName("created_utc")]
        [JsonProperty("created_utc")]
        [OpenApiProperty(Nullable = true, Description = "The date/time (UTC) that this seat was provisioned")]
        public DateTime? CreationDateTimeUtc { get; set; }

        [JsonPropertyName("redeemed_utc")]
        [JsonProperty("redeemed_utc")]
        [OpenApiProperty(Nullable = true, Description = "If this seat was originally reserved, the date/time (UTC) that the reservation was redeemed")]
        public DateTime? RedemptionDateTimeUtc { get; set; }
    }
}
