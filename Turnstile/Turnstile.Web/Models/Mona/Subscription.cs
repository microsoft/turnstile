using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Turnstile.Web.Models.Mona
{
    public class Subscription
    {
        [JsonProperty("subscriptionId")]
        [JsonPropertyName("subscriptionId")]
        public string? SubscriptionId { get; set; }

        [JsonProperty("subscriptionName")]
        [JsonPropertyName("subscriptionName")]
        public string? SubscriptionName { get; set; }

        [JsonProperty("offerId")]
        [JsonPropertyName("offerId")]
        public string? OfferId { get; set; }

        [JsonProperty("planId")]
        [JsonPropertyName("planId")]
        public string? PlanId { get; set; }

        [JsonProperty("isTest")]
        [JsonPropertyName("isTest")]
        public bool? IsTest { get; set; }

        [JsonProperty("isFreeTrial")]
        [JsonPropertyName("isFreeTrial")]
        public bool? IsFreeTrial { get; set; }

        [JsonProperty("seatQuantity")]
        [JsonPropertyName("seatQuantity")]
        public int? SeatQuantity { get; set; }

        [JsonProperty("status")]
        [JsonPropertyName("status")]
        public SubscriptionStatus Status { get; set; }

        [JsonProperty("term")]
        [JsonPropertyName("term")]
        public MarketplaceTerm? Term { get; set; }

        [JsonProperty("beneficiary")]
        [JsonPropertyName("beneficiary")]
        public MarketplaceUser? Beneficiary { get; set; }

        [JsonProperty("purchaser")]
        [JsonPropertyName("purchaser")]
        public MarketplaceUser? Purchaser { get; set; }

        public IEnumerable<string> Validate()
        {
            if (string.IsNullOrEmpty(SubscriptionId))
            {
                yield return "Mona subscription [subscription_id] is required.";
            }

            if (string.IsNullOrEmpty(OfferId))
            {
                yield return "Mona subscription [offer_id] is required.";
            }

            if (string.IsNullOrEmpty(PlanId))
            {
                yield return "Mona subscription [plan_id] is required.";
            }

            if (string.IsNullOrEmpty(Beneficiary?.AadTenantId))
            {
                // This is a super-important piece of information to have.
                // We rely on this piece of information to tie customers to subscription at an identity level.

                yield return "Mona subscription [beneficiary.tenant_id] is required.";
            }
        }
    }
}
