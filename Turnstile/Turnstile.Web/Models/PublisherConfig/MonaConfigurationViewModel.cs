using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Turnstile.Core.Constants;
using Turnstile.Core.Models.Configuration;

namespace Turnstile.Web.Models.PublisherConfig
{
    public class MonaConfigurationViewModel : BaseConfigurationViewModel
    {
        public const string DefaultStateIsMonaState = "Current Mona subscription state";

        public MonaConfigurationViewModel() { }

        public MonaConfigurationViewModel(PublisherConfiguration publisherConfig)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));

            MonaIntegrationBaseStorageUrl = publisherConfig.MonaBaseStorageUrl;
            DefaultMonaSubscriptionState = publisherConfig.DefaultMonaSubscriptionState;
            MonaSubscriptionIsBeingConfigured = publisherConfig.MonaSubscriptionIsBeingConfigured;
        }

        [Display(Name = "Mona integration base storage URL")]
        [Url(ErrorMessage = "Mona integration base storage URL is invalid.")]
        public string? MonaIntegrationBaseStorageUrl { get; set; }

        [Display(Name = "Forwarded Mona subscription default state")]
        public string? DefaultMonaSubscriptionState { get; set; }

        [Display(Name = "Forwarded Mona subscription is currently being configured")]
        public bool MonaSubscriptionIsBeingConfigured { get; set; } = false;

        public List<SelectListItem> DefaultMonaSubscriptionStates { get; set; } = new List<SelectListItem>
        {
            new SelectListItem(DefaultStateIsMonaState, DefaultStateIsMonaState),
            new SelectListItem(nameof(SubscriptionStates.Active), SubscriptionStates.Active),
            new SelectListItem(nameof(SubscriptionStates.Purchased), SubscriptionStates.Purchased),
            new SelectListItem(nameof(SubscriptionStates.Suspended), SubscriptionStates.Suspended),
            new SelectListItem(nameof(SubscriptionStates.Canceled), SubscriptionStates.Canceled)
        };
    }
}
