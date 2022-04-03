using Turnstile.Core.Models;
using Turnstile.Core.Models.Configuration;

namespace Turnstile.Web.Models
{
    public class SubscriptionMessageViewModel
    {
        public SubscriptionMessageViewModel() { }

        public SubscriptionMessageViewModel(PublisherConfiguration publisherConfig, Subscription subscription, bool isTenantAdmin = false)
            : this(publisherConfig, isTenantAdmin)
        {
            
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));

            Apply(subscription);
            
        }

        public SubscriptionMessageViewModel(PublisherConfiguration publisherConfig, bool isTenantAdmin = false)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));

            IsTenantSubscriptionAdministrator = isTenantAdmin;

            Apply(publisherConfig);
        }

        private void Apply(PublisherConfiguration publisherConfig)
        {
            if (!string.IsNullOrEmpty(publisherConfig.ContactSalesUrl))
            {
                SalesContactHtml = $"<a href=\"{publisherConfig.ContactSalesUrl}\">visit our sales page</a>";
            }
            else if (!string.IsNullOrEmpty(publisherConfig.ContactSalesEmail))
            {
                SalesContactHtml = $"<a href=\"mailto:{publisherConfig.ContactSalesEmail}\">contact sales</a>";
            }
            else
            {
                SalesContactHtml = "contact sales";
            }

            if (!string.IsNullOrEmpty(publisherConfig.ContactSupportUrl))
            {
                SupportContactHtml = $"<a href=\"{publisherConfig.ContactSupportUrl}\">visit our support page</a>";
            }
            else if (!string.IsNullOrEmpty(publisherConfig.ContactSupportEmail))
            {
                SupportContactHtml = $"<a href=\"mailto:{publisherConfig.ContactSupportEmail}\">contact support</a>";
            }
            else
            {
                SalesContactHtml = "contact support";
            }
        }

        private void Apply(Subscription subscription)
        {
            SubscriptionId = subscription.SubscriptionId;
            SubscriptionName = subscription.SubscriptionName;

            if (string.IsNullOrEmpty(subscription.AdminName))
            {
                SubscriptionAdminContactHtml = "subscription administrator";
            }
            else
            {
                SubscriptionAdminContactHtml = subscription.AdminName;
            }

            if (!string.IsNullOrEmpty(subscription.AdminEmail))
            {
                SubscriptionAdminContactHtml = $"<a href=\"mailto:{subscription.AdminEmail}\">{SubscriptionAdminContactHtml}</a>";
            }
        }

        public bool IsTenantSubscriptionAdministrator { get; set; } = false;

        public string? SubscriptionId { get; set; }
        public string? SubscriptionName { get; set; }

        public string? SubscriptionAdminContactHtml { get; set; }
        public string? SalesContactHtml { get; set; }
        public string? SupportContactHtml { get; set; }
    }
}
