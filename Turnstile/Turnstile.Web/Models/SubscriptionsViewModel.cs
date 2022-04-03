using Turnstile.Core.Models;

namespace Turnstile.Web.Models
{
    public class SubscriptionsViewModel
    {
        public SubscriptionsViewModel() { }

        public SubscriptionsViewModel(IEnumerable<Subscription> subscriptions, string? forTenantId = null)
        {
            ArgumentNullException.ThrowIfNull(subscriptions, nameof(subscriptions));

            Subscriptions = subscriptions.Select(s => new SubscriptionRowViewModel(s)).ToList();
            ForTenantId = forTenantId;
        }

        public string? ForTenantId { get; set; }

        public List<SubscriptionRowViewModel> Subscriptions { get; set; } = new List<SubscriptionRowViewModel>();
    }
}
