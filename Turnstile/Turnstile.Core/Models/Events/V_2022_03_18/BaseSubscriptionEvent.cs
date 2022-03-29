using System.Text.Json.Serialization;
using Turnstile.Core.Constants;
using Turnstile.Core.Interfaces;

namespace Turnstile.Core.Models.Events.V_2022_03_18
{
    public abstract class BaseSubscriptionEvent : ISubscriptionEvent
    {
        protected BaseSubscriptionEvent(string eventType)
        {
            ArgumentNullException.ThrowIfNull(eventType, nameof(eventType));

            EventType = eventType;
        }

        protected BaseSubscriptionEvent(string eventType, Subscription subscription)
        {
            ArgumentNullException.ThrowIfNull(eventType, nameof(eventType));
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));

            EventType = eventType;
            Subscription = subscription;
        }

        [JsonPropertyName("event_id")]
        public string EventId { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("event_type")]
        public string EventType { get; set; }

        [JsonPropertyName("event_version")]
        public string EventVersion { get; set; } = EventVersions.V_2022_03_18;

        [JsonPropertyName("occurred_utc")]
        public DateTime OccurredDateTimeUtc { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("subscription")]
        public Subscription? Subscription { get; set; }
    }
}
