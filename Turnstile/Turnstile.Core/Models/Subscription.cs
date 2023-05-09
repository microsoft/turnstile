// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
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
        [OpenApiProperty(Nullable = false, Description = "Unique subscription identifier")]
        public string? SubscriptionId { get; set; }

        [JsonPropertyName("subscription_name")]
        [JsonProperty("subscription_name")]
        [OpenApiProperty(Nullable = true, Description = "This subscription's friendly/human-readable name")]
        public string? SubscriptionName { get; set; }

        [JsonPropertyName("tenant_id")]
        [JsonProperty("tenant_id")]
        [OpenApiProperty(Nullable = false, Description = "Tenant (ID) that this subscription belongs to")]
        public string? TenantId { get; set; }

        [JsonPropertyName("tenant_name")]
        [JsonProperty("tenant_name")]
        [OpenApiProperty(Nullable = true, Description = "Tenant (name) that this subscription belongs to")]
        public string? TenantName { get; set; }

        [JsonPropertyName("offer_id")]
        [JsonProperty("offer_id")]
        [OpenApiProperty(Nullable = false, Description = "This subscription's offer ID")]
        public string? OfferId { get; set; }

        [JsonPropertyName("plan_id")]
        [JsonProperty("plan_id")]
        [OpenApiProperty(Nullable = false, Description = "This subscription's plan ID")]
        public string? PlanId { get; set; }

        [JsonPropertyName("state")]
        [JsonProperty("state")]
        [OpenApiProperty(Nullable = false, Description = "The current state of this subscription; options include [purchased], [active], [suspended], or [canceled]")]
        public string? State { get; set; }

        [JsonPropertyName("admin_role_name")]
        [JsonProperty("admin_role_name")]
        [OpenApiProperty(Nullable = true, Description = "The name of the role that users must belong to to administer this subscription")]
        public string? AdminRoleName { get; set; }

        [JsonPropertyName("user_role_name")]
        [JsonProperty("user_role_name")]
        [OpenApiProperty(Nullable = true, Description = "The name of the role that users must belong to to use this subscription")]
        public string? UserRoleName { get; set; }

        [JsonPropertyName("management_urls")]
        [JsonProperty("management_urls")]
        [OpenApiProperty(Nullable = true, Description = "A set of key (title)/value (URL) pairs used for managing this subscription")]
        public Dictionary<string, string>? ManagementUrls { get; set; }

        [JsonPropertyName("admin_name")]
        [JsonProperty("admin_name")]
        [OpenApiProperty(Nullable = true, Description = "The name of this subscription's administrator")]
        public string? AdminName { get; set; }

        [JsonPropertyName("admin_email")]
        [JsonProperty("admin_email")]
        [OpenApiProperty(Nullable = true, Description = "The email address of this subscription's administrator")]
        public string? AdminEmail { get; set; }

        [JsonPropertyName("total_seats")]
        [JsonProperty("total_seats")]
        [OpenApiProperty(Nullable = true, Description = "If configured for user-based seating, the total number of seats available in this subscription")]
        public int? TotalSeats { get; set; }

        [JsonPropertyName("is_being_configured")]
        [JsonProperty("is_being_configured")]
        [OpenApiProperty(Nullable = false, Description = "Whether or not this subscription is currently being configured; subscriptions can not be used if they're being configured")]
        public bool? IsBeingConfigured { get; set; }

        [JsonPropertyName("is_free_trial")]
        [JsonProperty("is_free_trial")]
        [OpenApiProperty(Nullable = false, Description = "Whether or not this is a free subscription")]
        public bool IsFreeTrial { get; set; } = false;

        [JsonPropertyName("is_setup_complete")]
        [JsonProperty("is_setup_complete")]
        [OpenApiProperty(Nullable = false, Description = "Whether or not this subscription has been set up by the subscriber")]
        public bool? IsSetupComplete { get; set; }

        [JsonPropertyName("is_test_subscription")]
        [JsonProperty("is_test_subscription")]
        [OpenApiProperty(Nullable = false, Description = "Whether or not this is a test subscription")]
        public bool IsTestSubscription { get; set; } = false;

        [JsonPropertyName("created_utc")]
        [JsonProperty("created_utc")]
        [OpenApiProperty(Nullable = true, Description = "The date/time (UTC) that this subscription was provisioned")]
        public DateTime? CreatedDateTimeUtc { get; set; }

        [JsonPropertyName("state_last_updated_utc")]
        [JsonProperty("state_last_updated_utc")]
        [OpenApiProperty(Nullable = true, Description = "The date/time (UTC) that this subscription's state was last changed")]
        public DateTime? StateLastUpdatedDateTimeUtc { get; set; }

        [JsonPropertyName("seating_config")]
        [JsonProperty("seating_config")]
        [OpenApiProperty(Nullable = true, Description = "This subscription's current seating strategy")]
        public SeatingConfiguration? SeatingConfiguration { get; set; }

        [JsonPropertyName("subscriber_info")]
        [JsonProperty("subscriber_info")]
        [OpenApiProperty(Nullable = true, Description = "JSON object property bag for additional publisher-specific metadata")]
        public JObject? SubscriberInfo { get; set; }

        [JsonPropertyName("source_subscription")]
        [JsonProperty("source_subscription")]
        [OpenApiProperty(Nullable = true, Description = "This subscription's source object model; e.g., Mona (https://github.com/microsoft/mona-saas) if purchased through Microsoft Marketplace")]
        public JObject? SourceSubscription { get; set; }

        public bool IsActive() => State == SubscriptionStates.Active; // Just a little convenience method.

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
                patch.AdminName == null &&
                patch.AdminEmail == null &&
                patch.ManagementUrls == null &&
                patch.TotalSeats == null &&
                patch.IsBeingConfigured == null &&
                patch.IsSetupComplete == null &&
                patch.SeatingConfiguration == null &&
                patch.SubscriberInfo == null &&
                patch.TenantName == null &&
                patch.SourceSubscription == null)
            {
                errors.Add(
                    "No subscription properties have been patched; patchable subscription properties are " +
                    $"[subscription_name], [plan_id], [state] (must be {SubscriptionStates.ValidStates.ToOrList()}), [tenant_name], " +
                    "[admin_role_name], [user_role_name], [management_urls], [total_seats] (if [total_seats] has already been set), " +
                    "[is_being_configured], [is_setup_complete], [seating_config], [subscriber_info], [source_subscription], [admin_name], and [admin_email].");
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
