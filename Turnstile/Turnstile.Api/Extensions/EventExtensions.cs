using Azure.Messaging.EventGrid;
using System;
using Turnstile.Core.Interfaces;

namespace Turnstile.Api.Extensions
{
    public static class EventExtensions
    {
        public static EventGridEvent ToEventGridEvent<TEvent>(this TEvent @event) where TEvent : ISubscriptionEvent
        {
            ArgumentNullException.ThrowIfNull(@event, nameof(@event));

            return new EventGridEvent(
                $"saas/subscriptions/{@event.Subscription!.SubscriptionId!}",
                @event.EventType,
                @event.EventVersion,
                @event);
        }
    }
}
