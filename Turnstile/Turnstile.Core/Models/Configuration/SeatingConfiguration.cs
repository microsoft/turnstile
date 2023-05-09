// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using Turnstile.Core.Constants;
using Turnstile.Core.Extensions;

namespace Turnstile.Core.Models.Configuration
{
    public class SeatingConfiguration
    {
        [JsonPropertyName("seating_strategy_name")]
        [JsonProperty("seating_strategy_name")]
        [OpenApiProperty(Nullable = false, Description = "The strategy used by this turnstile to provide seats; either [first_come_first_served] or [monthly_active_user]")]
        public string? SeatingStrategyName { get; set; }

        [JsonPropertyName("low_seat_warning_level_pct")]
        [JsonProperty("low_seat_warning_level_pct")]
        [OpenApiProperty(Nullable = false, Description = "The percentage of available seats that must be reached in order to trigger the [seat_warning_level_reached] event; by default, 25% (.25)")]
        public double? LowSeatWarningLevelPercent { get; set; } = .25;

        [JsonPropertyName("limited_overflow_seating_enabled")]
        [JsonProperty("limited_overflow_seating_enabled")]
        [OpenApiProperty(Nullable = false, Description = "Determines whether [limited] seats will be distributed when there are no more available [standard] seats; by default, false")]
        public bool? LimitedOverflowSeatingEnabled { get; set; }

        [JsonPropertyName("seat_reservation_expiry_in_days")]
        [JsonProperty("seat_reservation_expiry_in_days")]
        [OpenApiProperty(Nullable = false, Description = "The number of days that a seat will remain reserved until it is either redeemed or automatically released")]
        public int? SeatReservationExpiryInDays { get; set; } = 14;

        [JsonPropertyName("default_seat_expiry_in_days")]
        [JsonProperty("default_seat_expiry_in_days")]
        [OpenApiProperty(Nullable = false, Description = "The number of days that a seat will remain occupied (using the [first_come_first_served] seating strategy) before it is automatically released")]
        public int? DefaultSeatExpiryInDays { get; set; } = 14;

        public IEnumerable<string> Validate()
        {
            if (string.IsNullOrEmpty(SeatingStrategyName) ||
                !SeatingStrategies.ValidStrategies.Contains(SeatingStrategyName!.ToLower()))
            {
                yield return $"Seating configuration [seating_strategy_name] is required and must be {SeatingStrategies.ValidStrategies.ToOrList()}.";
            }

            if (LowSeatWarningLevelPercent != null && (LowSeatWarningLevelPercent < 0 || LowSeatWarningLevelPercent > 1))
            {
                yield return "if provided, seating configuration [low_seat_warning_level_pct] must be > 0 (0%) and < 1 (100%).";
            }

            if (SeatReservationExpiryInDays.GetValueOrDefault() < 1)
            {
                yield return "Seating configuration [seat_reservation_expiry_in_days] must be >= 1 day.";
            }

            if (DefaultSeatExpiryInDays.GetValueOrDefault() < 1)
            {
                yield return "Seating configuration [default_seat_expiry_in_days] must be >= day.";
            }
        }
    }
}
