using Microsoft.AspNetCore.Html;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Configuration;
using Turnstile.Web.Extensions;

namespace Turnstile.Web.Models
{
    public class SubscriptionContextViewModel
    {
        public SubscriptionContextViewModel(PublisherConfiguration publisherConfig, Subscription subscription, ClaimsPrincipal userPrincipal)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));
            ArgumentNullException.ThrowIfNull(userPrincipal, nameof(userPrincipal));

            SubscriptionId = subscription.SubscriptionId;
            State = subscription.State;

            IsBeingConfigured = subscription.IsBeingConfigured == true;
            CanUserAdministerSubscription = userPrincipal.CanAdministerSubscription(subscription);
            CanUserAdministerTurnstile = userPrincipal.CanAdministerTurnstile();

            ContactSalesHtml = CreateContactSalesHtml(publisherConfig);
            ContactSupportHtml = CreateContactSupportHtml(publisherConfig);
            ContactSubscriptionAdminHtml = CreateContactSubscriptionAdminHtml(subscription);

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

        public bool IsBeingConfigured { get; set; } = false;
        public bool CanUserAdministerSubscription { get; set; } = false;
        public bool CanUserAdministerTurnstile { get; set; } = false;

        public HtmlString? ContactSalesHtml { get; set; }
        public HtmlString? ContactSupportHtml { get; set; }
        public HtmlString? ContactSubscriptionAdminHtml { get; set; }

        [Display(Name = "Subscription created date/time (UTC)")]
        public DateTime? CreatedUtc { get; set; }

        [Display(Name = "Subscription state last updated date/time (UTC)")]
        public DateTime? StateLastUpdatedUtc { get; set; }

        public Dictionary<string, string> ManagementLinks { get; set; } = new Dictionary<string, string>();

        private HtmlString CreateContactSalesHtml(PublisherConfiguration publisherConfig)
        {
            if (!string.IsNullOrEmpty(publisherConfig.ContactSalesUrl))
            {
                return new HtmlString($@"<a href=""{publisherConfig.ContactSalesUrl}"">visit our sales page</a>");
            }
            else if (!string.IsNullOrEmpty(publisherConfig.ContactSalesEmail))
            {
                return new HtmlString($@"<a href=""mailto:{publisherConfig.ContactSalesEmail}"">contact sales</a>");
            }
            else
            {
                return new HtmlString("contact sales");
            }
        }

        private HtmlString CreateContactSupportHtml(PublisherConfiguration publisherConfig)
        {
            if (!string.IsNullOrEmpty(publisherConfig.ContactSupportUrl))
            {
                return new HtmlString($@"<a href=""{publisherConfig.ContactSupportUrl}"">visit our support page</a>");
            }
            else if (!string.IsNullOrEmpty(publisherConfig.ContactSupportEmail))
            {
                return new HtmlString($@"<a href=""mailto:{publisherConfig.ContactSupportEmail}"">contact support</a>");
            }
            else
            {
                return new HtmlString("contact support");
            }
        }

        private HtmlString CreateContactSubscriptionAdminHtml(Subscription subscription)
        {
            if (!string.IsNullOrEmpty(subscription.AdminName) &&
                !string.IsNullOrEmpty(subscription.AdminEmail))
            {
                return new HtmlString($@"contact <a href=""mailto:{subscription.AdminEmail}"">{subscription.AdminName}</a>");
            }
            else if (!string.IsNullOrEmpty(subscription.AdminEmail))
            {
                return new HtmlString($@"contact <a href=""mailto:{subscription.AdminEmail}"">your subscription administrator</a>");
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
