using System.Text.Json.Serialization;
using Turnstile.Core.Constants;

namespace Turnstile.Core.Models.Events.V_2022_03_18
{
    public class AdmissionGranted : BaseSeatEvent
    {
        public AdmissionGranted()
            : base(EventTypes.AdmissionGranted) { }

        public AdmissionGranted(Subscription subscription, Seat seat, SeatRequest seatRequest)
            : base(EventTypes.AdmissionGranted, subscription, seat)
        {
            ArgumentNullException.ThrowIfNull(seatRequest, nameof(seatRequest));

            SeatRequest = seatRequest;
        }

        [JsonPropertyName("seat_request")]
        public SeatRequest? SeatRequest { get; set; }
    }
}
