using System.Security.Claims;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Configuration;

namespace Turnstile.Web.Models
{
    public class PickSubscriptionViewModel : SubscriptionsViewModel
    {
        public PickSubscriptionViewModel() { }

        public PickSubscriptionViewModel(PublisherConfiguration publisherConfig, IEnumerable<Subscription> subscriptions, ClaimsPrincipal userPrincipal, string? returnTo = null)
            : base(publisherConfig, subscriptions, userPrincipal) =>
            ReturnToUrl = returnTo;

        public string? ReturnToUrl { get; set; }
    }
}
