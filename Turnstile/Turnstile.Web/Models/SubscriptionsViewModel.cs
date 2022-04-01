using Turnstile.Core.Constants;
using Turnstile.Core.Models;

namespace Turnstile.Web.Models
{
    public class SubscriptionsViewModel
    {
        public SubscriptionsViewModel() { }

        public SubscriptionsViewModel(IEnumerable<Subscription> subscriptions, string? forTenantId = null)
        {
            ArgumentNullException.ThrowIfNull(subscriptions, nameof(subscriptions));

            ActiveSubscriptions = subscriptions
                .Where(s => s.State == SubscriptionStates.Active)
                .Select(s => new SubscriptionRowViewModel(s))
                .ToList();

            PurchasedSubscriptions = subscriptions
                .Where(s => s.State == SubscriptionStates.Purchased)
                .Select(s => new SubscriptionRowViewModel(s))
                .ToList();

            SuspendedSubscriptions = subscriptions
                .Where(s => s.State == SubscriptionStates.Suspended)
                .Select(s => new SubscriptionRowViewModel(s))
                .ToList();

            CanceledSubscriptions = subscriptions
                .Where(s => s.State == SubscriptionStates.Canceled)
                .Select(s => new SubscriptionRowViewModel(s))
                .ToList();

            ForTenantId = forTenantId;
        }

        public string? ForTenantId { get; set; }

        public List<SubscriptionRowViewModel> ActiveSubscriptions { get; set; } = new List<SubscriptionRowViewModel>();
        public List<SubscriptionRowViewModel> PurchasedSubscriptions { get; set; } = new List<SubscriptionRowViewModel>();
        public List<SubscriptionRowViewModel> SuspendedSubscriptions { get; set; } = new List<SubscriptionRowViewModel>();
        public List<SubscriptionRowViewModel> CanceledSubscriptions { get; set; } = new List<SubscriptionRowViewModel>();
    }
}
