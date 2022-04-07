using Turnstile.Core.Models;

namespace Turnstile.Web.Models
{
    public class SubscriptionDetailViewModel
    {
        public SubscriptionDetailViewModel() { }

        public SubscriptionDetailViewModel(Subscription subscription, IEnumerable<Seat> seats, bool userIsTurnstileAdmin = false, bool userIsSubscriberAdmin = false)
        {
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));
            ArgumentNullException.ThrowIfNull(seats, nameof(seats));

            SubscriptionId = subscription.SubscriptionId;
            SubscriptionName = subscription.SubscriptionName;
            TenantId = subscription.TenantId;
            TenantName = subscription.TenantName;
            State = subscription.State;
            OfferId = subscription.OfferId;
            PlanId = subscription.PlanId;
            AdminRoleName = subscription.AdminRoleName;
            UserRoleName = subscription.UserRoleName;
            AdminName = subscription.AdminName;
            AdminEmail = subscription.AdminEmail;

            IsBeingConfigured = subscription.IsBeingConfigured == true;
            IsTestSubscription = subscription.IsTestSubscription;
            IsFreeSubscription = subscription.IsFreeTrial;
            UserIsTurnstileAdmin = userIsTurnstileAdmin;
            UserIsSubscriberAdmin = userIsSubscriberAdmin;

            CreatedDateTimeUtc = subscription.CreatedDateTimeUtc;
            StateLastUpdatedDateTimeUtc = subscription.StateLastUpdatedDateTimeUtc;

            ManagementUrls = subscription.ManagementUrls;

            Seating = new SeatsViewModel(subscription, seats);
        }

        public string? SubscriptionId { get; set; }
        public string? SubscriptionName { get; set; }
        public string? TenantId { get; set; }
        public string? TenantName { get; set; }
        public string? State { get; set; }
        public string? OfferId { get; set; }
        public string? PlanId { get; set; }
        public string? AdminRoleName { get; set; }
        public string? UserRoleName { get; set; }
        public string? AdminName { get; set; }
        public string? AdminEmail { get; set; }

        public bool IsBeingConfigured { get; set; }
        public bool IsTestSubscription { get; set; }
        public bool IsFreeSubscription { get; set; }
        public bool UserIsTurnstileAdmin { get; set; }
        public bool UserIsSubscriberAdmin { get; set; }

        public Dictionary<string, string>? ManagementUrls { get; set; }

        public DateTime? CreatedDateTimeUtc { get; set; }
        public DateTime? StateLastUpdatedDateTimeUtc { get; set; }

        public SeatsViewModel? Seating { get; set; }
    }
}
