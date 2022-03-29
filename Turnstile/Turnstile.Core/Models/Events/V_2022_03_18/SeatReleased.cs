using Turnstile.Core.Constants;

namespace Turnstile.Core.Models.Events.V_2022_03_18
{
    public class SeatReleased : BaseSeatEvent
    {
        public SeatReleased()
            : base(EventTypes.SeatReleased) { }

        public SeatReleased(Subscription subscription, Seat seat)
            : base(EventTypes.SeatReleased, subscription, seat) { }
    }
}
