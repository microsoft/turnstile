// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Turnstile.Core.Models;
using Turnstile.Web.Common.Models;

namespace Turnstile.Web.Models
{
    public class SubscriptionsViewModel
    {
        public SubscriptionsViewModel() { }

        public SubscriptionsViewModel(IEnumerable<Subscription> subscriptions) =>
            Subscriptions = subscriptions.Select(s => new SubscriptionContextViewModel(s)).ToList();

        public List<SubscriptionContextViewModel> Subscriptions { get; set; } = new List<SubscriptionContextViewModel>();
    }
}
