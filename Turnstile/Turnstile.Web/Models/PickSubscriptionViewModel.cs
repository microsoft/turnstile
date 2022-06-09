// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Claims;
using Turnstile.Core.Models;

namespace Turnstile.Web.Models
{
    public class PickSubscriptionViewModel
    {
        public PickSubscriptionViewModel() { }

        public PickSubscriptionViewModel(IEnumerable<Subscription> subscriptions, ClaimsPrincipal forPrincipal)
        {
            ArgumentNullException.ThrowIfNull(subscriptions, nameof(subscriptions));
            ArgumentNullException.ThrowIfNull(forPrincipal, nameof(forPrincipal));

            Subscriptions = subscriptions.Select(s => new SubscriptionIdentityViewModel(s, forPrincipal)).ToList();
        }

        public List<SubscriptionIdentityViewModel> Subscriptions { get; set; } = new List<SubscriptionIdentityViewModel>(); 
    }
}
