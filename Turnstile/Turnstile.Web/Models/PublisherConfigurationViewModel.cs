using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Turnstile.Web.Models
{
    public class PublisherConfigurationViewModel
    {
        [Display(Name = "Turnstile name")]
        [Required(ErrorMessage = "Turnstile name is required.")]
        public string? TurnstileName { get; set; }

        [Display(Name = "Publisher name")]
        [Required(ErrorMessage = "Publisher name is required.")]
        public string? PublisherName { get; set; }

        [Display(Name = "Publisher home page URL")]
        [Required(ErrorMessage = "Publisher home page URL is required.")]
        [Url(ErrorMessage = "Publisher home page URL is not a valid URL.")]
        public string? HomePageUrl { get; set; }

        [Display(Name = "Publisher contact page URL")]
        [Url(ErrorMessage = "Publisher contact page URL is not a valid URL.")]
        public string? ContactPageUrl { get; set; }

        [Display(Name = "Publisher privacy notice page URL")]
        [Url(ErrorMessage = "Publisher privacy notice page URL is not a valid URL.")]
        public string? PrivacyNoticePageUrl { get; set; }

        [Display(Name = "Publisher subscription sales URL")]
        [Url(ErrorMessage = "Publisher subscription sales URL is not a valid URL.")]
        public string? ContactSalesUrl { get; set; }

        [Display(Name = "Publisher subscription support URL")]
        [Url(ErrorMessage = "Publisher subscription support URL is not a valid URL.")]
        public string? ContactSupportUrl { get; set; }

        [Display(Name = "Publisher subscription sales email")]
        [EmailAddress(ErrorMessage = "Publisher subscription sales email is invalid.")]
        public string? ContactSalesEmail { get; set; }

        [Display(Name = "Publisher subscription support email")]
        [EmailAddress(ErrorMessage = "Publisher subscription support email is invalid.")]
        public string? ContactSupportEmail { get; set; }

        [Display(Name = "Mona integration base storage URL")]
        [Url(ErrorMessage = "Mona integration base storage URL is not a valid URL.")]
        public string? MonaIntegrationBaseStorageUrl { get; set; }

        public SeatingConfigurationViewModel SeatingConfiguration { get; set; } = new SeatingConfigurationViewModel();

        public TurnstileConfigurationViewModel TurnstileConfiguration { get; set; } = new TurnstileConfigurationViewModel();
    }
}
