using System.Security.Claims;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Configuration;
using Turnstile.Web.Common.Extensions;
using Turnstile.Web.Common.Models;

namespace Turnstile.Web.Models
{
    public class PickSubscriptionViewModel
    {
        public PickSubscriptionViewModel() { }

        public PickSubscriptionViewModel(IEnumerable<Subscription> subscriptions, ClaimsPrincipal userPrincipal, ClaimsConfiguration claimsConfig, string? returnTo = null)
        {
            ArgumentNullException.ThrowIfNull(subscriptions, nameof(subscriptions));
            ArgumentNullException.ThrowIfNull(claimsConfig, nameof(claimsConfig));
            ArgumentNullException.ThrowIfNull(userPrincipal, nameof(userPrincipal));

            ManageableSubscriptions = subscriptions
                .Where(s => userPrincipal.CanAdministerSubscription(claimsConfig, s))
                .Select(s => new SubscriptionContextViewModel(s, userPrincipal, claimsConfig))
                .ToList();

            UsableSubscriptions = subscriptions
                .Where(s => userPrincipal.CanUseSubscription(claimsConfig, s) && s.IsUsable())
                .Select(s => new SubscriptionContextViewModel(s, userPrincipal, claimsConfig))
                .ToList();

            ReturnToUrl = returnTo;
        }

        public List<SubscriptionContextViewModel> ManageableSubscriptions { get; set; } = new List<SubscriptionContextViewModel>();

        public List<SubscriptionContextViewModel> UsableSubscriptions { get; set; } = new List<SubscriptionContextViewModel>();

        public string? ReturnToUrl { get; set; }

        public bool Any() =>
            ManageableSubscriptions?.Any() == true ||
            UsableSubscriptions?.Any() == true;
    }
}
