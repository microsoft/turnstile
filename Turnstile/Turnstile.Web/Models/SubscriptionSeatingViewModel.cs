using Turnstile.Core.Constants;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Configuration;

namespace Turnstile.Web.Models
{
    public class SubscriptionSeatingViewModel
    {
        public SubscriptionSeatingViewModel(PublisherConfiguration publisherConfig, Subscription subscription, IEnumerable<Seat> seats)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));
            ArgumentNullException.ThrowIfNull(seats, nameof(seats));

            Seats = seats.Select(s => new SeatViewModel(s)).ToList();
            UsedSeats = seats.Count(s => s.SeatType == SeatTypes.Standard);

            if (subscription.TotalSeats != null)
            {
                TotalSeats = subscription.TotalSeats;

                if (UsedSeats > 0)
                {
                    UsedSeatsPct = (double)UsedSeats / TotalSeats;

                    if (UsedSeats >= TotalSeats)
                    {
                        SubscriptionHasNoMoreSeats = true;
                    }
                    else if (UsedSeatsPct >= .75)
                    {
                        SubscriptionHasLimitedSeats = true;
                    }
                }
                else
                {
                    UsedSeatsPct = 0;
                }
            }
        }

        public bool SubscriptionHasNoMoreSeats { get; set; } = false;
        public bool SubscriptionHasLimitedSeats { get; set; } = false;

        public double? UsedSeatsPct { get; set; } = null;

        public int? TotalSeats { get; set; } = null;
        public int UsedSeats { get; set; } = 0;

        public List<SeatViewModel> Seats { get; set; } = new List<SeatViewModel>();
    }
}
