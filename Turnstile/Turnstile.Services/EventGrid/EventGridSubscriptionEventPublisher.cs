using Azure.Identity;
using Azure.Messaging.EventGrid;
using Turnstile.Core.Interfaces;

namespace Turnstile.Services.EventGrid
{
    public class EventGridSubscriptionEventPublisher : ISubscriptionEventPublisher
    {
        private readonly EventGridPublisherClient eventGridClient;

        public EventGridSubscriptionEventPublisher(EventGridConfiguration eventGridConfig) =>
            eventGridClient = new EventGridPublisherClient(
                new Uri(eventGridConfig.TopicEndpoint!), 
                new DefaultAzureCredential());

        public async Task PublishEvent<TEvent>(TEvent subscriptionEvent) where TEvent : ISubscriptionEvent =>
            await eventGridClient.SendEventAsync(new EventGridEvent(
                $"saas/subscriptions/{subscriptionEvent.Subscription!.SubscriptionId!}",
                subscriptionEvent.EventType,
                subscriptionEvent.EventVersion,
                subscriptionEvent));
    }
}