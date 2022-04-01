using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Turnstile.Web.Models
{
    public class TurnstileConfigurationViewModel
    {
        [DisplayName("If subscription access is denied, redirect user to...")]
        [Url(ErrorMessage = "Redirect URL is invalid.")]
        public string? OnAccessDeniedUrl { get; set; }

        [DisplayName("If there are no seats available, redirect user to...")]
        [Url(ErrorMessage = "Redrect URL is invalid.")]
        public string? OnNoSeatsAvailableUrl { get; set; }

        [DisplayName("If subscription isn't ready, redirect user to...")]
        [Url(ErrorMessage = "Redirect URL is invalid.")]
        public string? OnSubscriptionPurchasedUrl { get; set; }

        [DisplayName("If subscription is suspended, redirect user to...")]
        [Url(ErrorMessage = "Redirect URL is invalid.")]
        public string? OnSubscriptionSuspendedUrl { get; set; }

        [DisplayName("If subscription is canceled, redirect user to...")]
        [Url(ErrorMessage = "Redirect URL is invalid.")]
        public string? OnSubscriptionCanceledUrl { get; set; }

        [DisplayName("If subscription not found, redirect user to...")]
        [Url(ErrorMessage = "Redirect URL is invalid.")]
        public string? OnSubscriptionNotFoundUrl { get; set; }

        [DisplayName("If no subscriptions found, redirect user to...")]
        [Url(ErrorMessage = "Redirect URL is invalid.")]
        public string? OnNoSubscriptionsFoundUrl { get; set; }

        [DisplayName("If a seat is available, redirect user to...")]
        [Required(ErrorMessage = "Redirect URL is required.")]
        [Url(ErrorMessage = "Redirect URL is invalid.")]
        public string? OnAccessGrantedUrl { get; set; }
    }
}
