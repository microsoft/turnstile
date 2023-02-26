// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Claims;
using Turnstile.Core.Models;

namespace Turnstile.Web.Models;

public class PickSubscriptionViewModel
{
    public PickSubscriptionViewModel() { }

    public PickSubscriptionViewModel(IEnumerable<Subscription> subscriptions, ClaimsPrincipal forPrincipal, string? returnTo = null)
    {
        ArgumentNullException.ThrowIfNull(subscriptions, nameof(subscriptions));
        ArgumentNullException.ThrowIfNull(forPrincipal, nameof(forPrincipal));

        Subscriptions = subscriptions.Select(s => new SubscriptionIdentityViewModel(s, forPrincipal, returnTo)).ToList();
    }

    public List<SubscriptionIdentityViewModel> Subscriptions { get; set; } = new List<SubscriptionIdentityViewModel>(); 
}
