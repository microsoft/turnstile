using System.Security.Claims;
using Turnstile.Core.Models;
using Turnstile.Web.Extensions;

namespace Turnstile.Web.Models
{
    public class PickSubscriptionViewModel
    {
        public PickSubscriptionViewModel() { }

        public PickSubscriptionViewModel(IEnumerable<Subscription> subscriptions, ClaimsPrincipal userPrincipal, string? returnTo = null)
        {
            ArgumentNullException.ThrowIfNull(subscriptions, nameof(subscriptions));
            ArgumentNullException.ThrowIfNull(userPrincipal, nameof(userPrincipal));

            ManageableSubscriptions = subscriptions
                .Where(s => userPrincipal.CanAdministerSubscription(s))
                .Select(s => new SubscriptionContextViewModel(s, userPrincipal))
                .ToList();

            UsableSubscriptions = subscriptions
                .Where(s => userPrincipal.CanUseSubscription(s) && s.IsActive() && s.IsSetupComplete == true)
                .Select(s => new SubscriptionContextViewModel(s, userPrincipal))
                .ToList();

            ReturnToUrl = returnTo;
        }

        public List<SubscriptionContextViewModel> ManageableSubscriptions { get; set; } = new List<SubscriptionContextViewModel>();

        public List<SubscriptionContextViewModel> UsableSubscriptions { get; set; } = new List<SubscriptionContextViewModel>();

        public string? ReturnToUrl { get; set; }
    }
}
