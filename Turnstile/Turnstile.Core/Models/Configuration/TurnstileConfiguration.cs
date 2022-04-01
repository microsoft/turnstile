using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Turnstile.Core.Models.Configuration
{
    public class TurnstileConfiguration
    {
        [JsonProperty("on_access_denied_url")]
        [JsonPropertyName("on_access_denied_url")]
        public string? OnAccessDeniedUrl { get; set; }

        [JsonProperty("on_access_granted_url")]
        [JsonPropertyName("on_access_granted_url")]
        public string? OnAccessGrantedUrl { get; set; }

        [JsonProperty("on_no_seat_available_url")]
        [JsonPropertyName("on_no_seat_available_url")]
        public string? OnNoSeatAvailableUrl { get; set; }

        [JsonProperty("on_subscription_not_ready_url")]
        [JsonPropertyName("on_subscription_not_ready_url")]
        public string? OnSubscriptionNotReadyUrl { get; set; }

        [JsonProperty("on_subscription_canceled_url")]
        [JsonPropertyName("on_subscription_canceled_url")]
        public string? OnSubscriptionCanceledUrl { get; set; }

        [JsonProperty("on_subscription_suspended_url")]
        [JsonPropertyName("on_subscription_suspended_url")]
        public string? OnSubscriptionSuspendedUrl { get; set; }

        [JsonProperty("on_subscription_not_found_url")]
        [JsonPropertyName("on_subscription_not_found_url")]
        public string? OnSubscriptionNotFoundUrl { get; set; }

        [JsonProperty("on_no_subscriptions_found_url")]
        [JsonPropertyName("on_no_subscriptions_found_url")]
        public string? OnNoSubscriptionsFoundUrl { get; set; }

        public IEnumerable<string> Validate()
        {
            if (string.IsNullOrEmpty(OnAccessGrantedUrl))
            {
                yield return "[on_access_granted_url] is required.";
            }
        }
    }
}
