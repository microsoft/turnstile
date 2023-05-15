using System.Text.Json.Serialization;
using Turnstile.Core.Constants;

namespace Turnstile.Core.Models.Events.V_2022_03_18
{
    public class AdmissionDenied : BaseSubscriptionEvent
    {
        public AdmissionDenied()
            : base(EventTypes.AdmissionDenied) { }

        public AdmissionDenied(Subscription subscription, SeatRequest seatRequest, string resultCode)
            : base(EventTypes.AdmissionDenied, subscription)
        {
            ArgumentNullException.ThrowIfNull(seatRequest, nameof(seatRequest));
            ArgumentNullException.ThrowIfNull(resultCode, nameof(resultCode));

            SeatRequest = seatRequest;
            ResultCode = resultCode;
        }

        [JsonPropertyName("seat_request")]
        public SeatRequest? SeatRequest { get; set; }

        [JsonPropertyName("result_code")]
        public string? ResultCode { get; set; }
    }
}
