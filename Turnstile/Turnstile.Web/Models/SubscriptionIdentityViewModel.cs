using System.Security.Claims;
using Turnstile.Core.Models;
using Turnstile.Web.Extensions;

namespace Turnstile.Web.Models
{
    public class SubscriptionIdentityViewModel
    {
        public SubscriptionIdentityViewModel() { }

        public SubscriptionIdentityViewModel(Subscription subscription, ClaimsPrincipal forPrincipal)
        {
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));
            ArgumentNullException.ThrowIfNull(forPrincipal, nameof(forPrincipal));

            CanAdminister = forPrincipal.CanAdministerSubscription(subscription);
            SubscriptionId = subscription.SubscriptionId;
            SubscriptionName = subscription.SubscriptionName;
        }

        public bool? CanAdminister { get; set; }

        public string? SubscriptionId { get; set; }
        public string? SubscriptionName { get; set; }
    }
}
