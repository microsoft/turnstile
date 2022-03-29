using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Turnstile.Services.Cosmos
{
    public class CosmosSeatCount
    {
        [JsonPropertyName("seat_count")]
        [JsonProperty("seat_count")]
        public int SeatCount { get; set; } = 0;

        [JsonPropertyName("seat_type")]
        [JsonProperty("seat_type")]
        public string? SeatType { get; set; }
    }
}
