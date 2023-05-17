using Newtonsoft.Json.Linq;
using System.Security.Claims;
using Turnstile.Core.Models;
using Turnstile.Web.Models;

namespace Turnstile.Web.Extensions
{
    public static class SubscriptionExtensions
    {
        public static Subscription ApplyUpdateBy(this Subscription subscription, SubscriptionDetailViewModel subscriptionDetail, ClaimsPrincipal byUser)
        {
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));
            ArgumentNullException.ThrowIfNull(subscriptionDetail, nameof(subscriptionDetail));
            ArgumentNullException.ThrowIfNull(byUser, nameof(byUser));

            subscription.AdminEmail = subscriptionDetail.AdminEmail;
            subscription.AdminName = subscriptionDetail.AdminName;
            subscription.AdminRoleName = subscriptionDetail.AdminRoleName ?? string.Empty;
            subscription.SubscriptionName = subscriptionDetail.SubscriptionName;
            subscription.TenantName = subscriptionDetail.TenantName;
            subscription.UserRoleName = subscriptionDetail.UserRoleName;
            subscription.SubscriberInfo = JObject.FromObject(new SubscriberInfo(subscriptionDetail.TenantCountry));

            if (byUser.CanAdministerTurnstile())
            {
                subscription.IsTestSubscription = subscriptionDetail.IsTestSubscription;
                subscription.IsFreeTrial = subscriptionDetail.IsFreeTrialSubscription;
                subscription.OfferId = subscriptionDetail.OfferId;
                subscription.PlanId = subscriptionDetail.PlanId;
            }

            return subscription;
        }
    }
}
