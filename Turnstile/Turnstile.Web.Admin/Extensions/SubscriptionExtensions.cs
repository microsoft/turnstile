using Newtonsoft.Json.Linq;
using Turnstile.Core.Models;
using Turnstile.Web.Admin.Models;

namespace Turnstile.Web.Admin.Extensions
{
    public static class SubscriptionExtensions
    {
        public static Subscription ApplyUpdate(this Subscription subscription, SubscriptionDetailViewModel subscriptionDetail)
        {
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));
            ArgumentNullException.ThrowIfNull(subscriptionDetail, nameof(subscriptionDetail));

            subscription.AdminEmail = subscriptionDetail.AdminEmail;
            subscription.AdminName = subscriptionDetail.AdminName;
            subscription.AdminRoleName = subscriptionDetail.AdminRoleName ?? string.Empty;
            subscription.SubscriptionName = subscriptionDetail.SubscriptionName;
            subscription.TenantName = subscriptionDetail.TenantName;
            subscription.UserRoleName = subscriptionDetail.UserRoleName ?? string.Empty;
            subscription.SubscriberInfo = JObject.FromObject(new SubscriberInfo(subscriptionDetail.TenantCountry));
            subscription.IsTestSubscription = subscriptionDetail.IsTestSubscription;
            subscription.IsFreeTrial = subscriptionDetail.IsFreeTrialSubscription;
            subscription.State = subscriptionDetail.State;
            subscription.OfferId = subscriptionDetail.OfferId;
            subscription.PlanId = subscriptionDetail.PlanId;

            return subscription;
        }
    }
}
