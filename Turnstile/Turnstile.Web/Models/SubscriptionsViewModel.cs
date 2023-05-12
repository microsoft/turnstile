// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Claims;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Configuration;

namespace Turnstile.Web.Models
{
    public class SubscriptionsViewModel
    {
        public SubscriptionsViewModel() { }

        public SubscriptionsViewModel(PublisherConfiguration publisherConfig, IEnumerable<Subscription> subscriptions, ClaimsPrincipal userPrincipal)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));
            ArgumentNullException.ThrowIfNull(subscriptions, nameof(subscriptions));
            ArgumentNullException.ThrowIfNull(userPrincipal, nameof(userPrincipal));

            Subscriptions = subscriptions.Select(s => new SubscriptionContextViewModel(publisherConfig, s, userPrincipal)).ToList();
        }

        public List<SubscriptionContextViewModel> Subscriptions { get; set; } = new List<SubscriptionContextViewModel>();
    }
}
