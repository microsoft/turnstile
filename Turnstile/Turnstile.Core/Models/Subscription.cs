using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json.Serialization;
using Turnstile.Core.Constants;
using Turnstile.Core.Extensions;
using Turnstile.Core.Models.Configuration;

namespace Turnstile.Core.Models
{
    public class Subscription
    {
        [JsonPropertyName("subscription_id")]
        [JsonProperty("subscription_id")]
        public string? SubscriptionId { get; set; }

        [JsonPropertyName("subscription_name")]
        [JsonProperty("subscription_name")]
        public string? SubscriptionName { get; set; }

        [JsonPropertyName("tenant_id")]
        [JsonProperty("tenant_id")]
        public string? TenantId { get; set; }

        [JsonPropertyName("offer_id")]
        [JsonProperty("offer_id")]
        public string? OfferId { get; set; }

        [JsonPropertyName("plan_id")]
        [JsonProperty("plan_id")]
        public string? PlanId { get; set; }

        [JsonPropertyName("state")]
        [JsonProperty("state")]
        public string? State { get; set; }

        [JsonPropertyName("admin_role_name")]
        [JsonProperty("admin_role_name")]
        public string? AdminRoleName { get; set; }

        [JsonPropertyName("user_role_name")]
        [JsonProperty("user_role_name")]
        public string? UserRoleName { get; set; }

        [JsonPropertyName("management_urls")]
        [JsonProperty("management_urls")]
        public Dictionary<string, string>? ManagementUrls { get; set; }

        [JsonPropertyName("total_seats")]
        [JsonProperty("total_seats")]
        public int? TotalSeats { get; set; }

        [JsonPropertyName("is_being_configured")]
        [JsonProperty("is_being_configured")]
        public bool? IsBeingConfigured { get; set; }

        [JsonPropertyName("is_free_trial")]
        [JsonProperty("is_free_trial")]
        public bool IsFreeTrial { get; set; } = false;

        [JsonPropertyName("is_setup_complete")]
        [JsonProperty("is_setup_complete")]
        public bool? IsSetupComplete { get; set; }

        [JsonPropertyName("is_test_subscription")]
        [JsonProperty("is_test_subscription")]
        public bool IsTestSubscription { get; set; } = false;

        [JsonPropertyName("created_utc")]
        [JsonProperty("created_utc")]
        public DateTime? CreatedDateTimeUtc { get; set; }

        [JsonPropertyName("state_last_updated_utc")]
        [JsonProperty("state_last_updated_utc")]
        public DateTime? StateLastUpdatedDateTimeUtc { get; set; }

        [JsonPropertyName("seating_config")]
        [JsonProperty("seating_config")]
        public SeatingConfiguration? SeatingConfiguration { get; set; }

        [JsonPropertyName("subscriber_info")]
        [JsonProperty("subscriber_info")]
        public JObject? SubscriberInfo { get; set; }

        [JsonPropertyName("source_subscription")]
        [JsonProperty("source_subscription")]
        public JObject? SourceSubscription { get; set; }

        public IEnumerable<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(SubscriptionId))
            {
                errors.Add("Subscription [subscription_id] is required.");
            }

            if (string.IsNullOrEmpty(TenantId))
            {
                errors.Add("Subscription [tenant_id] is required.");
            }

            if (string.IsNullOrEmpty(OfferId))
            {
                errors.Add("Subscription [offer_id] is required.");
            }

            if (string.IsNullOrEmpty(PlanId))
            {
                errors.Add("Subscription [plan_id] is required.");
            }

            if (string.IsNullOrEmpty(State))
            {
                errors.Add("Subscription [state] is required.");
            }
            else
            {
                errors.AddRange(SubscriptionStates.ValidateState(State!));
            }

            if (SeatingConfiguration != null)
            {
                errors.AddRange(SeatingConfiguration!.Validate().Select(e => $"Subscription [seating_config]: {e}"));
            }

            return errors;
        }

        public IEnumerable<string> ValidatePatch(Subscription patch)
        {
            ArgumentNullException.ThrowIfNull(patch, nameof(patch));

            var errors = new List<string>();

            if (patch.SubscriptionName == null &&
                patch.PlanId == null &&
                patch.State == null &&
                patch.AdminRoleName == null &&
                patch.UserRoleName == null &&
                patch.ManagementUrls == null &&
                patch.TotalSeats == null &&
                patch.IsBeingConfigured == null &&
                patch.IsSetupComplete == null &&
                patch.SeatingConfiguration == null &&
                patch.SubscriberInfo == null &&
                patch.SourceSubscription == null)
            {
                errors.Add(
                    "No subscription properties have been patched; patchable subscription properties are " +
                    $"[subscription_name], [plan_id], [state] (must be {SubscriptionStates.ValidStates.ToOrList()}), " +
                    "[admin_role_name], [user_role_name], [management_urls], [total_seats] (if [total_seats] has already been set), " +
                    "[is_being_configured], [is_setup_complete], [seating_config], [subscriber_info], and [source_subscription].");
            }
            else
            {
                if (patch.SeatingConfiguration != null)
                {
                    errors.AddRange(patch.SeatingConfiguration!.Validate().Select(e => $"Subscription patch [seating_config]: {e}"));
                }

                if (!string.IsNullOrEmpty(patch.State))
                {
                    errors.AddRange(SubscriptionStates.ValidateState(patch.State!));
                }

                if (patch.TotalSeats != null)
                {
                    if (TotalSeats == null)
                    {
                        errors.Add("[total_seats] can be patched only on subscriptions that already have [total_seats] configured.");
                    }
                    else if (patch.TotalSeats <= TotalSeats)
                    {
                        errors.Add($"Patched [total_seats] ({patch.TotalSeats}) must be > existing total seats ({TotalSeats}).");
                    }
                }
            }

            return errors;
        }
    }
}
