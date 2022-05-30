using System.Runtime.CompilerServices;
using Turnstile.Core.Models;

namespace Turnstile.Core.Extensions
{
    public static class SubscriptionExtensions
    {
        public static bool HasReachedLowSeatWarningLevel(this Subscription subscription, SeatingSummary seatSummary)
        {
            const double lowSeatingWarningLevel = .25;

            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));
            ArgumentNullException.ThrowIfNull(seatSummary, nameof(seatSummary));

            return (subscription.TotalSeats.GetValueOrDefault() > 0 &&
                   (1 - (seatSummary.StandardSeatCount / (double)(subscription.TotalSeats!))) <= lowSeatingWarningLevel);
        }

        public static string GetDefaultAdminRoleName(this Subscription subscription) =>
            $"{subscription.SubscriptionId} Administrators";
    }
}
