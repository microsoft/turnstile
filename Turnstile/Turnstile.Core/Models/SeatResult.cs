using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Turnstile.Core.Models
{
    public class SeatResult
    {
        public SeatResult() { }

        public SeatResult(string resultCode, Subscription? subscription = null, Seat? seat = null)
        {
            ResultCode = resultCode;
            Subscription = subscription;
            Seat = seat;
        }

        [JsonProperty("result_code")]
        [JsonPropertyName("result_code")]
        public string? ResultCode { get; set; }

        [JsonProperty("seat")]
        [JsonPropertyName("seat")]
        public Seat? Seat { get; set; }

        [JsonProperty("subscription")]
        [JsonPropertyName("subscription")]
        public Subscription? Subscription { get; set; }
    }
}
