using Turnstile.Core.Constants;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Configuration;

namespace Turnstile.Web.Models
{
    public class SeatsViewModel
    {
        public SeatsViewModel() { }

        public SeatsViewModel(Subscription subscription, IEnumerable<Seat> seats)
        {
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));
            ArgumentNullException.ThrowIfNull(seats, nameof(seats));

            var seatList = seats.ToList();
            var seatConfig = subscription.SeatingConfiguration!;

            SeatingStrategyName = seatConfig.SeatingStrategyName;
            SeatExpiryInDays = seatConfig.DefaultSeatExpiryInDays;
            SeatReservationExpiryInDays = seatConfig.SeatReservationExpiryInDays;

            if (subscription.TotalSeats != null)
            {
                IsLimitedOverflowSeatingEnabled = seatConfig.LimitedOverflowSeatingEnabled == true;
                TotalSeats = subscription.TotalSeats!.Value;
                TotalUsedSeats = seatList.Count(s => s.SeatType == SeatTypes.Standard);

                ShowSeatingMeter = true;

                if (TotalSeats != 0 && TotalUsedSeats != 0)
                {
                    var pctConsumed = ((double)TotalUsedSeats / TotalSeats);

                    ConsumedSeatsPct = (int)(pctConsumed * 100);

                    if (TotalSeats <= TotalUsedSeats)
                    {
                        HasNoMoreSeats = true;
                    }
                    else if ((1 - pctConsumed) <= SeatingConfiguration.DefaultLowSeatWarningLevelPercent)
                    {
                        HasReachedLowSeatLevel = true;
                    }
                }
            }

            OccupiedSeats = seatList
                .Where(s => s.Occupant != null)
                .Select(s => new OccupiedSeatViewModel(s))
                .ToList();

            ReservedSeats = seatList
                .Where(s => s.Reservation != null)
                .Select(s => new ReservedSeatViewModel(s))
                .ToList();
        }

        public List<OccupiedSeatViewModel> OccupiedSeats { get; set; } = new List<OccupiedSeatViewModel>();
        public List<ReservedSeatViewModel> ReservedSeats { get; set; } = new List<ReservedSeatViewModel>();

        public int ConsumedSeatsPct { get; set; }
        public int TotalSeats { get; set; }
        public int TotalUsedSeats { get; set; }

        public int? SeatExpiryInDays { get; set; }
        public int? SeatReservationExpiryInDays { get; set; }

        public bool ShowSeatingMeter { get; set; }
        public bool HasNoMoreSeats { get; set; }
        public bool HasReachedLowSeatLevel { get; set; }

        public bool IsLimitedOverflowSeatingEnabled { get; set; }
        public bool IsSeatReserved { get; set; }
        public bool DidSeatReservationValidationFail { get; set; }

        public string? ReserveSeatForEmail { get; set; }
        public string? SeatingStrategyName { get; set; }
    }
}
