// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Turnstile.Core.Models;

namespace Turnstile.Core.Interfaces
{
    public interface ISubscriptionsClient
    {
        Task<Subscription?> GetSubscription(string subscriptionId);
        Task<Subscription?> CreateSubscription(Subscription subscription);
        Task<Subscription?> UpdateSubscription(Subscription subscription);
        Task<IEnumerable<Subscription>> GetSubscriptions(string? tenantId = null);
    }
}
