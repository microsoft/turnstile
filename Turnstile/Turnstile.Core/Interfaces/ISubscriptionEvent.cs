// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Turnstile.Core.Models;

namespace Turnstile.Core.Interfaces
{
    public interface ISubscriptionEvent
    {
        public string EventId { get; set; }
        public string EventType { get; set; }
        public string EventVersion { get; set; }

        DateTime OccurredDateTimeUtc { get; set; }

        Subscription? Subscription { get; set; }
    }
}
