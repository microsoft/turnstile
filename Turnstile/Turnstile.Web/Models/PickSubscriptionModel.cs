using System.Security.Claims;
using Turnstile.Core.Models;

namespace Turnstile.Web.Models
{
    public class PickSubscriptionModel
    {
        public PickSubscriptionModel() { }

        public PickSubscriptionModel(IEnumerable<Subscription> subscriptions, ClaimsPrincipal forPrincipal)
        {
            ArgumentNullException.ThrowIfNull(subscriptions, nameof(subscriptions));
            ArgumentNullException.ThrowIfNull(forPrincipal, nameof(forPrincipal));

            Subscriptions = subscriptions.Select(s => new SubscriptionIdentityModel(s, forPrincipal)).ToList();
        }

        public List<SubscriptionIdentityModel> Subscriptions { get; set; } = new List<SubscriptionIdentityModel>(); 
    }
}
