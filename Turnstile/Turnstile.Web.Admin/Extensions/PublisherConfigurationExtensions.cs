// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;
using Turnstile.Core.Models.Configuration;
using Turnstile.Web.Admin.Models.PublisherConfig;

namespace Turnstile.Web.Admin.Extensions
{
    public static class PublisherConfigurationExtensions
    {
        public static PublisherConfiguration Apply(this PublisherConfiguration publisherConfig, BasicConfigurationViewModel basicConfig)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));
            ArgumentNullException.ThrowIfNull(basicConfig, nameof(basicConfig));

            publisherConfig.AppUrl = basicConfig.AppUrl;
            publisherConfig.ContactPageUrl = basicConfig.ContactPageUrl;
            publisherConfig.ContactSalesEmail = basicConfig.ContactSalesEmail;
            publisherConfig.ContactSalesUrl = basicConfig.ContactSalesUrl;
            publisherConfig.ContactSupportEmail = basicConfig.ContactSupportEmail;
            publisherConfig.ContactSupportUrl = basicConfig.ContactSupportUrl;
            publisherConfig.HomePageUrl = basicConfig.HomePageUrl;
            publisherConfig.PrivacyNoticePageUrl = basicConfig.PrivacyNoticePageUrl;
            publisherConfig.PublisherName = basicConfig.PublisherName;
            publisherConfig.TurnstileName = basicConfig.TurnstileName;
            publisherConfig.TurnstileConfiguration ??= new TurnstileConfiguration(); // Default redirection configuration
            publisherConfig.SeatingConfiguration ??= new SeatingConfiguration();     // Default seating configuration

            return publisherConfig;
        }

        public static PublisherConfiguration Apply(this PublisherConfiguration publisherConfig, ClaimsConfigurationViewModel claimsConfigModel)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));
            ArgumentNullException.ThrowIfNull(claimsConfigModel, nameof(claimsConfigModel));

            publisherConfig.ClaimsConfiguration = new ClaimsConfiguration
            {
                UserIdClaimTypes = ParseClaimTypes(claimsConfigModel.UserIdClaimTypes),
                UserNameClaimTypes = ParseClaimTypes(claimsConfigModel.UserNameClaimTypes),
                TenantIdClaimTypes = ParseClaimTypes(claimsConfigModel.TenantIdClaimTypes),
                EmailClaimTypes = ParseClaimTypes(claimsConfigModel.EmailClaimTypes),
                RoleClaimTypes = ParseClaimTypes(claimsConfigModel.RoleClaimTypes)
            };

            return publisherConfig;
        }

        private static string[]? ParseClaimTypes(string? claimTypes) =>
            claimTypes?.Split(",").Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();

        public static PublisherConfiguration Apply(this PublisherConfiguration publisherConfig, MonaConfigurationViewModel monaConfig)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));
            ArgumentNullException.ThrowIfNull(monaConfig, nameof(monaConfig));

            publisherConfig.MonaBaseStorageUrl = monaConfig.MonaIntegrationBaseStorageUrl;
            publisherConfig.DefaultMonaSubscriptionState = monaConfig.DefaultMonaSubscriptionState;
            publisherConfig.MonaSubscriptionIsBeingConfigured = monaConfig.MonaSubscriptionIsBeingConfigured;

            return publisherConfig;
        }

        public static PublisherConfiguration Apply(this PublisherConfiguration publisherConfig, RedirectConfigurationViewModel redirectConfig)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));
            ArgumentNullException.ThrowIfNull(redirectConfig, nameof(redirectConfig));

            publisherConfig.TurnstileConfiguration = new TurnstileConfiguration
            {
                OnNoSeatAvailableUrl = redirectConfig.OnNoSeatsAvailableUrl,
                OnAccessDeniedUrl = redirectConfig.OnAccessDeniedUrl,
                OnNoSubscriptionsFoundUrl = redirectConfig.OnNoSubscriptionsFoundUrl,
                OnSubscriptionCanceledUrl = redirectConfig.OnSubscriptionCanceledUrl,
                OnSubscriptionNotFoundUrl = redirectConfig.OnSubscriptionNotFoundUrl,
                OnSubscriptionNotReadyUrl = redirectConfig.OnSubscriptionNotReadyUrl,
                OnSubscriptionSuspendedUrl = redirectConfig.OnSubscriptionSuspendedUrl
            };

            return publisherConfig;
        }

        public static PublisherConfiguration Apply(this PublisherConfiguration publisherConfig, SeatingConfigurationViewModel seatingConfig)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));
            ArgumentNullException.ThrowIfNull(seatingConfig, nameof(seatingConfig));

            publisherConfig.SeatingConfiguration ??= new SeatingConfiguration();
            publisherConfig.SeatingConfiguration.LimitedOverflowSeatingEnabled = seatingConfig.LimitedOverflowSeatingEnabled;

            return publisherConfig;
        }
    }
}
