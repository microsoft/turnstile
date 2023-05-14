using System.ComponentModel.DataAnnotations;
using Turnstile.Core.Models.Configuration;

namespace Turnstile.Web.Models.PublisherConfig
{
    public class BasicConfigurationViewModel : BaseConfigurationViewModel
    {
        public BasicConfigurationViewModel() { }

        public BasicConfigurationViewModel(PublisherConfiguration publisherConfig)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));

            TurnstileName = publisherConfig.TurnstileName;
            PublisherName = publisherConfig.PublisherName;
            HomePageUrl = publisherConfig.HomePageUrl;
            ContactPageUrl = publisherConfig.ContactPageUrl;
            PrivacyNoticePageUrl = publisherConfig.PrivacyNoticePageUrl;
            ContactSalesUrl = publisherConfig.ContactSalesUrl;
            ContactSupportUrl = publisherConfig.ContactSupportUrl;
            ContactSalesEmail = publisherConfig.ContactSalesEmail;
            ContactSupportEmail = publisherConfig.ContactSupportEmail;
        }

        [Display(Name = "App name")]
        [Required(ErrorMessage = "App name is required.")]
        public string? TurnstileName { get; set; }

        [Display(Name = "Publisher name")]
        [Required(ErrorMessage = "Publisher name is required.")]
        public string? PublisherName { get; set; }

        [Display(Name = "Publisher home page URL")]
        [Required(ErrorMessage = "Publisher home page URL is required.")]
        [Url(ErrorMessage = "Publisher home page URL is invalid.")]
        public string? HomePageUrl { get; set; }

        [Display(Name = "Publisher contact page URL")]
        [Url(ErrorMessage = "Publisher contact page URL is invalid.")]
        public string? ContactPageUrl { get; set; }

        [Display(Name = "Publisher privacy notice page URL")]
        [Url(ErrorMessage = "Publisher privacy notice page URL is invalid.")]
        public string? PrivacyNoticePageUrl { get; set; }

        [Display(Name = "Subscription sales URL")]
        [Url(ErrorMessage = "Subscription sales URL is invalid.")]
        public string? ContactSalesUrl { get; set; }

        [Display(Name = "Subscription support URL")]
        [Url(ErrorMessage = "Subscription support URL is invalid.")]
        public string? ContactSupportUrl { get; set; }

        [Display(Name = "Subscription sales email address")]
        [EmailAddress(ErrorMessage = "Subscription sales email address is invalid.")]
        public string? ContactSalesEmail { get; set; }

        [Display(Name = "Subscription support email address")]
        [EmailAddress(ErrorMessage = "Subscription support email is invalid.")]
        public string? ContactSupportEmail { get; set; }
    }
}
