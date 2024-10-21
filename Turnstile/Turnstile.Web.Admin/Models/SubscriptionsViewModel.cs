// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Claims;
using Turnstile.Core.Constants;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Configuration;
using Turnstile.Web.Common.Models;

namespace Turnstile.Web.Models
{
    public class SubscriptionsViewModel
    {
        public SubscriptionsViewModel() { }

        public SubscriptionsViewModel(IEnumerable<Subscription> subscriptions, ClaimsPrincipal userPrincipal, ClaimsConfiguration claimsConfig)
        {
            ArgumentNullException.ThrowIfNull(subscriptions, nameof(subscriptions));
            ArgumentNullException.ThrowIfNull(userPrincipal, nameof(userPrincipal));
            ArgumentNullException.ThrowIfNull(claimsConfig, nameof(claimsConfig));

            ActiveSubscriptions = subscriptions
                .Where(s => s.State == SubscriptionStates.Active || s.State == SubscriptionStates.Purchased)
                .Select(s => new SubscriptionContextViewModel(s, userPrincipal, claimsConfig))
                .ToList();

            SuspendedSubscriptions = subscriptions
                .Where(s => s.State == SubscriptionStates.Suspended)
                .Select(s => new SubscriptionContextViewModel(s, userPrincipal, claimsConfig))
                .ToList();

            CanceledSubscriptions = subscriptions
                .Where(s => s.State == SubscriptionStates.Canceled)
                .Select(s => new SubscriptionContextViewModel(s, userPrincipal, claimsConfig))
                .ToList();
        }

        public List<SubscriptionContextViewModel> ActiveSubscriptions { get; set; } = new List<SubscriptionContextViewModel>();
        public List<SubscriptionContextViewModel> SuspendedSubscriptions { get; set; } = new List<SubscriptionContextViewModel>();
        public List<SubscriptionContextViewModel> CanceledSubscriptions { get; set;} = new List<SubscriptionContextViewModel>();


    }
}
