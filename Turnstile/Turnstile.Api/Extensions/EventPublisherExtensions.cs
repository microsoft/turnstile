using Turnstile.Core.Extensions;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Events.V_2022_03_18;

namespace Turnstile.Api.Extensions
{
    public static class EventPublisherExtensions
    {
        public async static Task PublishSeatWarningEvents(
            this ISubscriptionEventPublisher eventPublisher,
            Subscription subscription,
            SeatingSummary seatingSummary)
        {
            ArgumentNullException.ThrowIfNull(eventPublisher, nameof(eventPublisher));
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));
            ArgumentNullException.ThrowIfNull(seatingSummary, nameof(seatingSummary));

            if (subscription.TotalSeats != null)
            {
                if (seatingSummary.StandardSeatCount >= subscription.TotalSeats)
                {
                    await eventPublisher.PublishEvent(new NoSeatsAvailable(subscription, seatingSummary));
                }
                else if (subscription.HasReachedLowSeatWarningLevel(seatingSummary))
                {
                    await eventPublisher.PublishEvent(new LowSeatWarningLevelReached(subscription, seatingSummary));
                }
            }
        }
    }
}
