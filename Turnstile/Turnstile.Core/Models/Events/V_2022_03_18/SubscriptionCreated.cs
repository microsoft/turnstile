// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Turnstile.Core.Constants;

namespace Turnstile.Core.Models.Events.V_2022_03_18;

public class SubscriptionCreated : BaseSubscriptionEvent
{
    public SubscriptionCreated()
        : base(EventTypes.SubscriptionCreated) { }

    public SubscriptionCreated(Subscription subscription)
        : base(EventTypes.SubscriptionCreated, subscription) { }
}
