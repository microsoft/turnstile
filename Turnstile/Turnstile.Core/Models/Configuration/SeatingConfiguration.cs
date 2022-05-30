using Newtonsoft.Json;
using System.Text.Json.Serialization;
using Turnstile.Core.Constants;
using Turnstile.Core.Extensions;

namespace Turnstile.Core.Models.Configuration
{
    public class SeatingConfiguration
    {
        public const double DefaultLowSeatWarningLevelPercent = .25;

        [JsonPropertyName("seating_strategy_name")]
        [JsonProperty("seating_strategy_name")]
        public string? SeatingStrategyName { get; set; }

        [JsonPropertyName("limited_overflow_seating_enabled")]
        [JsonProperty("limited_overflow_seating_enabled")]
        public bool? LimitedOverflowSeatingEnabled { get; set; }

        [JsonPropertyName("seat_reservation_expiry_in_days")]
        [JsonProperty("seat_reservation_expiry_in_days")]
        public int? SeatReservationExpiryInDays { get; set; }

        [JsonPropertyName("default_seat_expiry_in_days")]
        [JsonProperty("default_seat_expiry_in_days")]
        public int? DefaultSeatExpiryInDays { get; set; }

        public IEnumerable<string> Validate()
        {
            if (string.IsNullOrEmpty(SeatingStrategyName) ||
                !SeatingStrategies.ValidStrategies.Contains(SeatingStrategyName!.ToLower()))
            {
                yield return $"Seating configuration [seating_strategy_name] is required and must be {SeatingStrategies.ValidStrategies.ToOrList()}.";
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
