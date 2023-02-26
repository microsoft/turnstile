// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Turnstile.Core.Constants;
using Turnstile.Core.Interfaces;

namespace Turnstile.Core.Models.Events.V_2022_03_18;

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

    public class PropertiesViewModel
    {
        public PropertiesViewModel() { }

        public PropertiesViewModel(BaseSubscriptionEvent @event)
        {
            EventId = @event.EventId;
            EventType = @event.EventType;
            EventVersion = @event.EventVersion;
            OccurredDateTimeUtc = @event.OccurredDateTimeUtc;
            SubscriptionId = @event.Subscription?.SubscriptionId;
            SubscriptionName = @event.Subscription?.SubscriptionName;
            TenantId = @event.Subscription?.TenantId;
            TenantName = @event.Subscription?.TenantName;
            OfferId = @event.Subscription?.OfferId;
            PlanId = @event.Subscription?.PlanId;

        }

        [JsonPropertyName("Event ID")]
        public string? EventId { get; set; }

        [JsonPropertyName("Event Type")]
        public string? EventType { get; set; }

        [JsonPropertyName("Event Version")]
        public string? EventVersion { get; set; }

        [JsonPropertyName("Event Occurred Date/Time UTC")]
        public DateTime? OccurredDateTimeUtc { get; set; }

        [JsonPropertyName("Subscription ID")]
        public string? SubscriptionId { get; set; }

        [JsonPropertyName("Subscription Name")]
        public string? SubscriptionName { get; set; }

        [JsonPropertyName("Tenant ID")]
        public string? TenantId { get; set; }

        [JsonPropertyName("Tenant Name")]
        public string? TenantName { get; set; }

        [JsonPropertyName("Subscription Offer ID")]
        public string? OfferId { get; set; }

        [JsonPropertyName("Subscription Plan ID")]
        public string? PlanId { get; set; }

        [JsonPropertyName("Subscription State")]
        public string? SubscriptionState { get; set; }

        [JsonPropertyName("Subscription Administrator Role Name")]
        public string? SubscriptionAdminRoleName { get; set; }

        [JsonPropertyName("Subscription User Role Name")]
        public string? SubscriptionUserRoleName { get; set; }

        [JsonPropertyName("Subscription Management URLs")]
        public Dictionary<string, string> SubscriptionManagementUrls = new Dictionary<string, string>();

        [JsonPropertyName("Subscription Administrator Name")]
        public string? SubscriptionAdminName { get; set; }
    }
}
