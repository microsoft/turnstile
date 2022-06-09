// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Claims;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Configuration;

namespace Turnstile.Web.Models
{
    public class SubscriptionSetupViewModel
    {
        public SubscriptionSetupViewModel() { }

        public SubscriptionSetupViewModel(
            PublisherConfiguration publisherConfig,
            Subscription subscription,
            ClaimsPrincipal forPrincipal)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));
            ArgumentNullException.ThrowIfNull(forPrincipal, nameof(forPrincipal));

            SubscriptionId = subscription.SubscriptionId;
            SubscriberInfo = new SubscriberInfoViewModel(subscription, forPrincipal);
            SubscriptionConfiguration = new SubscriptionConfigurationViewModel(publisherConfig, subscription, forPrincipal);
        }

        public Subscription CreatePatch()

        {
            var patch = new Subscription() { SubscriptionId = SubscriptionId };

            SubscriberInfo?.ApplyTo(patch);
            SubscriptionConfiguration?.ApplyTo(patch);

            return patch;
        }

        public string? SubscriptionId { get; set; }

        public SubscriberInfoViewModel? SubscriberInfo { get; set; }

        public SubscriptionConfigurationViewModel? SubscriptionConfiguration { get; set; }
    }
}
