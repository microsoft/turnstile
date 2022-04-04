using Microsoft.Identity.Web;
using System.Security.Claims;
using Turnstile.Core.Models.Configuration;
using Turnstile.Web.Extensions;

namespace Turnstile.Web.Models
{
    public class LayoutViewModel
    {
        public LayoutViewModel() { }

        public LayoutViewModel(PublisherConfiguration publisherConfig, ClaimsPrincipal forPrincipal)
            : this(forPrincipal)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));
          
            TurnstileName = publisherConfig.TurnstileName;
            PublisherName = publisherConfig.PublisherName;
            HomePageUrl = publisherConfig.HomePageUrl;
            ContactPageUrl = publisherConfig.ContactPageUrl;
            PrivacyNoticePageUrl = publisherConfig.PrivacyNoticePageUrl;
        }

        public LayoutViewModel(ClaimsPrincipal forPrincipal)
        {
            ArgumentNullException.ThrowIfNull(forPrincipal, nameof(forPrincipal));

            IsTurnstileAdmin = forPrincipal.CanAdministerTurnstile();
            IsTenantSubscriptionAdmin = forPrincipal.CanAdministerAllTenantSubscriptions(forPrincipal.GetHomeTenantId()!);
        }

        public string? TurnstileName { get; set; }
        public string? PublisherName { get; set; }
        public string? HomePageUrl { get; set; }
        public string? ContactPageUrl { get; set; }
        public string? PrivacyNoticePageUrl { get; set; }

        public bool IsTurnstileAdmin { get; set; } = false;
        public bool IsTenantSubscriptionAdmin { get; set; } = false;
    }
}
