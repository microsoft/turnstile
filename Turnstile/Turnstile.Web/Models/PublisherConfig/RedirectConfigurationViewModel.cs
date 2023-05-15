using System.ComponentModel.DataAnnotations;
using Turnstile.Core.Models.Configuration;

namespace Turnstile.Web.Models.PublisherConfig
{
    public class RedirectConfigurationViewModel : BaseConfigurationViewModel
    {
        public RedirectConfigurationViewModel() { }

        public RedirectConfigurationViewModel(PublisherConfiguration publisherConfig)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));

            if (publisherConfig.TurnstileConfiguration != null)
            {
                var turnstileConfig = publisherConfig.TurnstileConfiguration;

                OnAccessDeniedUrl = turnstileConfig.OnAccessDeniedUrl;
                OnNoSeatsAvailableUrl = turnstileConfig.OnNoSeatAvailableUrl;
                OnSubscriptionPurchasedUrl = turnstileConfig.OnSubscriptionNotReadyUrl;
                OnSubscriptionSuspendedUrl = turnstileConfig.OnSubscriptionSuspendedUrl;
                OnSubscriptionCanceledUrl = turnstileConfig.OnSubscriptionCanceledUrl;
                OnSubscriptionNotFoundUrl = turnstileConfig.OnSubscriptionNotFoundUrl;
                OnNoSubscriptionsFoundUrl = turnstileConfig.OnNoSubscriptionsFoundUrl;
            }
        }

        public TurnstileConfiguration ToCoreModel() =>
            new TurnstileConfiguration
            {
                OnAccessDeniedUrl = OnAccessDeniedUrl,
                OnNoSeatAvailableUrl = OnNoSeatsAvailableUrl,
                OnSubscriptionNotReadyUrl = OnSubscriptionPurchasedUrl,
                OnSubscriptionSuspendedUrl = OnSubscriptionSuspendedUrl,
                OnSubscriptionCanceledUrl = OnSubscriptionCanceledUrl,
                OnSubscriptionNotFoundUrl = OnSubscriptionNotFoundUrl,
                OnNoSubscriptionsFoundUrl = OnNoSubscriptionsFoundUrl
            };

        [Url(ErrorMessage = "Redirect URL is invalid.")]
        public string? OnAccessDeniedUrl { get; set; }

        [Url(ErrorMessage = "Redrect URL is invalid.")]
        public string? OnNoSeatsAvailableUrl { get; set; }

        [Url(ErrorMessage = "Redirect URL is invalid.")]
        public string? OnSubscriptionPurchasedUrl { get; set; }

        [Url(ErrorMessage = "Redirect URL is invalid.")]
        public string? OnSubscriptionSuspendedUrl { get; set; }

        [Url(ErrorMessage = "Redirect URL is invalid.")]
        public string? OnSubscriptionCanceledUrl { get; set; }

        [Url(ErrorMessage = "Redirect URL is invalid.")]
        public string? OnSubscriptionNotFoundUrl { get; set; }

        [Url(ErrorMessage = "Redirect URL is invalid.")]
        public string? OnNoSubscriptionsFoundUrl { get; set; }
    }
}
