// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Turnstile.Core.Constants;

namespace Turnstile.Core.Models.Events.V_2022_03_18
{
    public class SubscriptionUpdated : BaseSubscriptionEvent
    {
        public SubscriptionUpdated()
            : base(EventTypes.SubscriptionUpdated) { }

        public SubscriptionUpdated(Subscription subscription)
            : base(EventTypes.SubscriptionUpdated, subscription) { }
    }
}
