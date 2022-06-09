// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Turnstile.Core.Constants;
using Turnstile.Core.Models.Configuration;

namespace Turnstile.Web.Models
{
    public class PublisherConfigurationViewModel
    {
        public const string DefaultStateIsMonaState = "Current Mona subscription state";

        public PublisherConfigurationViewModel() { }

        public PublisherConfigurationViewModel(PublisherConfiguration publisherConfig)
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
            MonaIntegrationBaseStorageUrl = publisherConfig.MonaBaseStorageUrl;
            MonaSubscriptionIsBeingConfigured = publisherConfig.MonaSubscriptionIsBeingConfigured;
            DefaultMonaSubscriptionState = publisherConfig.DefaultMonaSubscriptionState;
            SeatingConfiguration = new SeatingConfigurationViewModel(publisherConfig.SeatingConfiguration ?? new SeatingConfiguration());
            TurnstileConfiguration = new TurnstileConfigurationViewModel(publisherConfig.TurnstileConfiguration ?? new TurnstileConfiguration());
        }

        public PublisherConfiguration ToCoreModel(bool isSetupComplete) =>
            new PublisherConfiguration
            {
                TurnstileName = TurnstileName,
                PublisherName = PublisherName,
                HomePageUrl = HomePageUrl,
                ContactPageUrl = ContactPageUrl,
                PrivacyNoticePageUrl = PrivacyNoticePageUrl,
                ContactSalesUrl = ContactSalesUrl,
                ContactSupportUrl = ContactSupportUrl,
                ContactSalesEmail = ContactSalesEmail,
                ContactSupportEmail = ContactSupportEmail,
                MonaBaseStorageUrl = MonaIntegrationBaseStorageUrl,
                MonaSubscriptionIsBeingConfigured = MonaSubscriptionIsBeingConfigured,
                DefaultMonaSubscriptionState = (DefaultMonaSubscriptionState == DefaultStateIsMonaState ? null : DefaultMonaSubscriptionState),
                IsSetupComplete = isSetupComplete,
                SeatingConfiguration = SeatingConfiguration?.ToCoreModel(),
                TurnstileConfiguration = TurnstileConfiguration?.ToCoreModel()
            };

        [Display(Name = "Turnstile name")]
        [Required(ErrorMessage = "Turnstile name is required.")]
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

        [Display(Name = "Mona integration base storage URL")]
        [Url(ErrorMessage = "Mona integration base storage URL is invalid.")]
        public string? MonaIntegrationBaseStorageUrl { get; set; }

        public List<SelectListItem> DefaultMonaSubscriptionStates { get; set; } = new List<SelectListItem>
        {
            new SelectListItem(DefaultStateIsMonaState, DefaultStateIsMonaState),
            new SelectListItem(nameof(SubscriptionStates.Active), SubscriptionStates.Active),
            new SelectListItem(nameof(SubscriptionStates.Purchased), SubscriptionStates.Purchased),
            new SelectListItem(nameof(SubscriptionStates.Suspended), SubscriptionStates.Suspended),
            new SelectListItem(nameof(SubscriptionStates.Canceled), SubscriptionStates.Canceled)
        };

        [Display(Name = "Forwarded Mona subscription default state")]
        public string? DefaultMonaSubscriptionState { get; set; }

        [Display(Name = "Forwarded Mona subscription is currently being configured")]
        public bool MonaSubscriptionIsBeingConfigured { get; set; } = false;

        public SeatingConfigurationViewModel SeatingConfiguration { get; set; } = new SeatingConfigurationViewModel();

        public TurnstileConfigurationViewModel TurnstileConfiguration { get; set; } = new TurnstileConfigurationViewModel();

        public bool IsConfigurationSaved { get; set; } = false;

        public bool HasValidationErrors { get; set; } = false;
    }
}
