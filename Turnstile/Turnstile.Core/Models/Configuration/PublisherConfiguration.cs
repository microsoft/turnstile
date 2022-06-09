// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Turnstile.Core.Models.Configuration
{
    public class PublisherConfiguration
    {
        [JsonProperty("turnstile_name")]
        [JsonPropertyName("turnstile_name")]
        public string? TurnstileName { get; set; }

        [JsonProperty("publisher_name")]
        [JsonPropertyName("publisher_name")]
        public string? PublisherName { get; set; }

        [JsonProperty("home_page_url")]
        [JsonPropertyName("home_page_url")]
        public string? HomePageUrl { get; set; }

        [JsonProperty("contact_page_url")]
        [JsonPropertyName("contact_page_url")]
        public string? ContactPageUrl { get; set; }

        [JsonProperty("privacy_notice_page_url")]
        [JsonPropertyName("privacy_notice_page_url")]
        public string? PrivacyNoticePageUrl { get; set; }

        [JsonProperty("contact_sales_email")]
        [JsonPropertyName("contact_sales_email")]
        public string? ContactSalesEmail { get; set; }

        [JsonProperty("contact_sales_url")]
        [JsonPropertyName("contact_sales_url")]
        public string? ContactSalesUrl { get; set; }

        [JsonProperty("contact_support_email")]
        [JsonPropertyName("contact_support_email")]
        public string? ContactSupportEmail { get; set; }

        [JsonProperty("contact_support_url")]
        [JsonPropertyName("contact_support_url")]
        public string? ContactSupportUrl { get; set; }

        [JsonProperty("mona_base_storage_url")]
        [JsonPropertyName("mona_base_storage_url")]
        public string? MonaBaseStorageUrl { get; set; }

        [JsonProperty("mona_subscription_state")]
        [JsonPropertyName("mona_subscription_state")]
        public string? DefaultMonaSubscriptionState { get; set; }

        [JsonProperty("mona_subscription_is_being_configured")]
        [JsonPropertyName("mona_subscription_is_being_configured")]
        public bool MonaSubscriptionIsBeingConfigured { get; set; } = false;

        [JsonProperty("is_setup_complete")]
        [JsonPropertyName("is_setup_complete")]
        public bool IsSetupComplete { get; set; } = false;

        [JsonProperty("default_seating_config")]
        [JsonPropertyName("default_seating_config")]
        public SeatingConfiguration? SeatingConfiguration { get; set; }

        [JsonProperty("turnstile_config")]
        [JsonPropertyName("turnstile_config")]
        public TurnstileConfiguration? TurnstileConfiguration { get; set; }

        public IEnumerable<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(TurnstileName))
            {
                errors.Add("[turnstile_name] is required.");
            }

            if (string.IsNullOrEmpty(PublisherName))
            {
                errors.Add("[publisher_name] is required.");
            }

            if (SeatingConfiguration == null)
            {
                errors.Add("[default_seating_config] is required.");
            }
            else
            {
                errors.AddRange(SeatingConfiguration!.Validate().Select(e => $"Publisher configuration [default_seating_config]: {e}"));
            }

            if (TurnstileConfiguration == null)
            {
                errors.Add("[turnstile_config] is required.");
            }
            else
            {
                errors.AddRange(TurnstileConfiguration!.Validate().Select(e => $"Publisher configuration [turnstile_config]: {e}"));
            }

            return errors;
        }
    }
}
