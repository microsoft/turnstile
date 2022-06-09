// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Turnstile.Core.Extensions;

namespace Turnstile.Core.Constants
{
    public static class SubscriptionStates
    {
        public const string Purchased = "purchased";
        public const string Active = "active";
        public const string Suspended = "suspended";
        public const string Canceled = "canceled";

        public static readonly string[] ValidStates =
            new[] { Purchased, Active, Suspended, Canceled };

        public static IEnumerable<string> ValidateState(string subscriptionState)
        {
            if (!ValidStates.Contains(subscriptionState.ToLower()))
            {
                yield return
                    $"[{subscriptionState}] is not a valid subscription [state]; " +
                    $"subscription [state] must be {ValidStates.ToOrList()}";
            }
        }
    }
}
