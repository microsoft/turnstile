using Turnstile.Core.Models;

namespace Turnstile.Web.Models
{
    public class ReservedSeatViewModel
    {
        public ReservedSeatViewModel() { }

        public ReservedSeatViewModel(Seat seat)
        {
            ArgumentNullException.ThrowIfNull(seat, nameof(seat));

            SeatId = seat.SeatId;
            ReservedForEmail = seat.Reservation!.Email;
            ExpiresDateTimeUtc = seat.ExpirationDateTimeUtc;
            ReservedDateTimeUtc = seat.CreationDateTimeUtc;
        }

        public string? SeatId { get; set; }
        public string? ReservedForEmail { get; set; }

        public DateTime? ExpiresDateTimeUtc { get; set; }
        public DateTime? ReservedDateTimeUtc { get; set; }
    }
}
