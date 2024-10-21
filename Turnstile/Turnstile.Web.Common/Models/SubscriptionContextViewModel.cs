using Microsoft.AspNetCore.Html;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Configuration;
using Turnstile.Web.Common.Extensions;

namespace Turnstile.Web.Common.Models
{
    public class SubscriptionContextViewModel
    {
        public SubscriptionContextViewModel(Subscription subscription, ClaimsPrincipal userPrincipal, ClaimsConfiguration claimsConfig)
        {
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));
            ArgumentNullException.ThrowIfNull(userPrincipal, nameof(userPrincipal));
            ArgumentNullException.ThrowIfNull(claimsConfig, nameof(claimsConfig));

            SubscriptionId = subscription.SubscriptionId;
            SubscriptionName = subscription.SubscriptionName;
            State = subscription.State;
            TenantId = subscription.TenantId;
            TenantName = subscription.TenantName;

            IsBeingConfigured = (subscription.IsBeingConfigured == true);
            IsSetupComplete = (subscription.IsSetupComplete == true);
            IsFreeTrialSubscription = subscription.IsFreeTrial;
            IsTestSubscription = subscription.IsTestSubscription;
            ContactSubscriptionAdminHtml = CreateContactSubscriptionAdminHtml(subscription);

            CanUserAdministerSubscription = userPrincipal.CanAdministerSubscription(claimsConfig, subscription);
            CanUserUseSubscription = userPrincipal.CanUseSubscription(claimsConfig,subscription);

            if (subscription.ManagementUrls != null)
            {
                ManagementLinks = subscription.ManagementUrls;
            }

            CreatedUtc = subscription.CreatedDateTimeUtc;
            StateLastUpdatedUtc = subscription.StateLastUpdatedDateTimeUtc;
        }

        [Display(Name = "Subscription ID")]
        public string? SubscriptionId { get; set; }

        [Display(Name = "Subscription name")]
        public string? SubscriptionName { get; set; }

        [Display(Name = "Subscription state")]
        public string? State { get; set; }

        [Display(Name = "Tenant ID")]
        public string? TenantId { get; set; }

        [Display(Name = "Tenant name")]
        public string? TenantName { get; set; }

        public bool IsFreeTrialSubscription { get; set; } = false;
        public bool IsTestSubscription { get; set; } = false;

        public bool IsBeingConfigured { get; set; } = false;
        public bool IsSetupComplete { get; set; } = false;
        public bool CanUserAdministerSubscription { get; set; } = false;
        public bool CanUserUseSubscription { get; set; } = false;
        public bool CanUserAdministerTurnstile { get; set; } = false;


        public HtmlString? ContactSubscriptionAdminHtml { get; set; }

        [Display(Name = "Subscription created date/time (UTC)")]
        public DateTime? CreatedUtc { get; set; }

        [Display(Name = "Subscription state last updated date/time (UTC)")]
        public DateTime? StateLastUpdatedUtc { get; set; }

        public Dictionary<string, string> ManagementLinks { get; set; } = new Dictionary<string, string>();

        private HtmlString CreateContactSubscriptionAdminHtml(Subscription subscription)
        {
            if (!string.IsNullOrEmpty(subscription.AdminName) &&
                !string.IsNullOrEmpty(subscription.AdminEmail))
            {
                return new HtmlString($@"contact <a href=""mailto:{subscription.AdminEmail}"" class=""alert-link"">{subscription.AdminName}</a>");
            }
            else if (!string.IsNullOrEmpty(subscription.AdminEmail))
            {
                return new HtmlString($@"contact <a href=""mailto:{subscription.AdminEmail}"" class=""alert-link"">your subscription administrator</a>");
            }
            else if (!string.IsNullOrEmpty(subscription.AdminName))
            {
                return new HtmlString($@"contact {subscription.AdminName}");
            }
            else
            {
                return new HtmlString("contact your subscription adminstrator");
            }
        }
    }
}
