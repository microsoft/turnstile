namespace Turnstile.Core.Interfaces
{
    public interface ISubscriptionEventPublisher
    {
        Task PublishEvent<TEvent>(TEvent subscriptionEvent) where TEvent : ISubscriptionEvent;
    }
}
