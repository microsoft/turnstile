// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;
using Turnstile.Core.Models.Configuration;

namespace Turnstile.Web.Models
{
    public class TurnstileConfigurationViewModel
    {
        public TurnstileConfigurationViewModel() { }

        public TurnstileConfigurationViewModel(TurnstileConfiguration turnstileConfig)
        {
            ArgumentNullException.ThrowIfNull(turnstileConfig, nameof(turnstileConfig));

            OnAccessDeniedUrl = turnstileConfig.OnAccessDeniedUrl;
            OnNoSeatsAvailableUrl = turnstileConfig.OnNoSeatAvailableUrl;
            OnSubscriptionPurchasedUrl = turnstileConfig.OnSubscriptionNotReadyUrl;
            OnSubscriptionSuspendedUrl = turnstileConfig.OnSubscriptionSuspendedUrl;
            OnSubscriptionCanceledUrl = turnstileConfig.OnSubscriptionCanceledUrl;
            OnSubscriptionNotFoundUrl = turnstileConfig.OnSubscriptionNotFoundUrl;
            OnNoSubscriptionsFoundUrl = turnstileConfig.OnNoSubscriptionsFoundUrl;
            OnAccessGrantedUrl = turnstileConfig.OnAccessGrantedUrl;
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
                OnNoSubscriptionsFoundUrl = OnNoSubscriptionsFoundUrl,
                OnAccessGrantedUrl = OnAccessGrantedUrl
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

        [Required(ErrorMessage = "Redirect URL is required.")]
        [Url(ErrorMessage = "Redirect URL is invalid.")]
        public string? OnAccessGrantedUrl { get; set; }
    }
}
