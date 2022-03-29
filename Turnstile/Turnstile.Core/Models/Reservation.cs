using Newtonsoft.Json;
using System.Text.Json.Serialization;
using Turnstile.Core.Constants;

namespace Turnstile.Core.Models
{
    public class Reservation
    {
        [JsonPropertyName("user_id")]
        [JsonProperty("user_id")]
        public string? UserId { get; set; }

        [JsonPropertyName("tenant_id")]
        [JsonProperty("tenant_id")]
        public string? TenantId { get; set; }

        [JsonPropertyName("email")]
        [JsonProperty("email")]
        public string? Email { get; set; }

        public IEnumerable<string> Validate(Subscription inSubscription)
        {
            ArgumentNullException.ThrowIfNull(inSubscription, nameof(inSubscription));

            if (string.IsNullOrEmpty(Email) && (string.IsNullOrEmpty(TenantId) || string.IsNullOrEmpty(UserId)))
            {
                yield return "Reservation ([user_id] and [tenant_id]) or [email] is required.";
            }

            if (inSubscription.State != SubscriptionStates.Active)
            {
                yield return
                    $"Subscription [{inSubscription.SubscriptionId}] is currently [{inSubscription.State}]; " +
                    $"seats can be reserved only in [{SubscriptionStates.Active}] subscriptions.";
            }
        }
    }
}
