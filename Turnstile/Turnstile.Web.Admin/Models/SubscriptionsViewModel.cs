// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Claims;
using Turnstile.Core.Constants;
using Turnstile.Core.Models;
using Turnstile.Web.Common.Models;

namespace Turnstile.Web.Models
{
    public class SubscriptionsViewModel
    {
        public SubscriptionsViewModel() { }

        public SubscriptionsViewModel(IEnumerable<Subscription> subscriptions, ClaimsPrincipal userPrincipal)
        {
            ArgumentNullException.ThrowIfNull(subscriptions, nameof(subscriptions));
            ArgumentNullException.ThrowIfNull(userPrincipal, nameof(userPrincipal));

            ActiveSubscriptions = subscriptions
                .Where(s => s.State == SubscriptionStates.Active || s.State == SubscriptionStates.Purchased)
                .Select(s => new SubscriptionContextViewModel(s, userPrincipal))
                .ToList();

            SuspendedSubscriptions = subscriptions
                .Where(s => s.State == SubscriptionStates.Suspended)
                .Select(s => new SubscriptionContextViewModel(s, userPrincipal))
                .ToList();

            CanceledSubscriptions = subscriptions
                .Where(s => s.State == SubscriptionStates.Canceled)
                .Select(s => new SubscriptionContextViewModel(s, userPrincipal))
                .ToList();
        }

        public List<SubscriptionContextViewModel> ActiveSubscriptions { get; set; } = new List<SubscriptionContextViewModel>();
        public List<SubscriptionContextViewModel> SuspendedSubscriptions { get; set; } = new List<SubscriptionContextViewModel>();
        public List<SubscriptionContextViewModel> CanceledSubscriptions { get; set;} = new List<SubscriptionContextViewModel>();


    }
}
