using Turnstile.Core.Models;

namespace Turnstile.Core.Extensions
{
    public static class SubscriptionExtensions
    {
        public static bool HasReachedLowSeatWarningLevel(this Subscription subscription, SeatingSummary seatSummary)
        {
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));
            ArgumentNullException.ThrowIfNull(seatSummary, nameof(seatSummary));

            return (subscription.TotalSeats.GetValueOrDefault() > 0 &&
                    subscription.SeatingConfiguration?.LowSeatWarningLevelPercent != null &&
                    (1 - (seatSummary.StandardSeatCount / (double)(subscription.TotalSeats!))) <=
                    subscription.SeatingConfiguration.LowSeatWarningLevelPercent.Value);
        }

        public static string GetDefaultAdminRoleName(this Subscription subscription) =>
            $"{subscription.SubscriptionId} Administrators";
    }
}
