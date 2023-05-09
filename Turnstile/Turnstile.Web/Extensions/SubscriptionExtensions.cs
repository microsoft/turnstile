using Turnstile.Core.Models;
using Turnstile.Web.Models;

namespace Turnstile.Web.Extensions
{
    public static class SubscriptionExtensions
    {
        public static Subscription ApplyUpdate(this Subscription subscription, SubscriptionDetailViewModel subscriptionDetail)
        {
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));
            ArgumentNullException.ThrowIfNull(subscriptionDetail, nameof(subscriptionDetail));

            subscription.AdminEmail = subscriptionDetail.AdminEmail;
            subscription.AdminName = subscriptionDetail.AdminName;
            subscription.AdminRoleName = subscriptionDetail.AdminRoleName;
            subscription.IsBeingConfigured = subscriptionDetail.IsBeingConfigured;
            subscription.OfferId = subscriptionDetail.OfferId;
            subscription.PlanId = subscriptionDetail.PlanId;
            subscription.State = subscriptionDetail.State;
            subscription.SubscriptionName = subscriptionDetail.SubscriptionName;
            subscription.TenantName = subscriptionDetail.TenantName;
            subscription.UserRoleName = subscriptionDetail.UserRoleName;
            subscription.StateLastUpdatedDateTimeUtc = DateTime.UtcNow;

            return subscription;
        }
    }
}
