// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Turnstile.Core.Models.Configuration
{
    public class PublisherConfiguration
    {
        [JsonProperty("turnstile_name")]
        [JsonPropertyName("turnstile_name")]
        [OpenApiProperty(Nullable = false, Description = "This turnstile's display name")]
        public string? TurnstileName { get; set; }

        [JsonProperty("publisher_name")]
        [JsonPropertyName("publisher_name")]
        [OpenApiProperty(Nullable = false, Description = "The SaaS app publisher's display name")]
        public string? PublisherName { get; set; }

        [JsonProperty("home_page_url")]
        [JsonPropertyName("home_page_url")]
        [OpenApiProperty(Nullable = true, Description = "The SaaS app publisher's home page URL")]
        public string? HomePageUrl { get; set; }

        [JsonProperty("contact_page_url")]
        [JsonPropertyName("contact_page_url")]
        [OpenApiProperty(Nullable = true, Description = "The SaaS app publisher's contact page URL")]
        public string? ContactPageUrl { get; set; }

        [JsonProperty("privacy_notice_page_url")]
        [JsonPropertyName("privacy_notice_page_url")]
        [OpenApiProperty(Nullable = true, Description = "The SaaS app publisher's privacy notice page URL")]
        public string? PrivacyNoticePageUrl { get; set; }

        [JsonProperty("contact_sales_email")]
        [JsonPropertyName("contact_sales_email")]
        [OpenApiProperty(Nullable = true, Description = "The SaaS app publisher's subscription sales email address")]
        public string? ContactSalesEmail { get; set; }

        [JsonProperty("contact_sales_url")]
        [JsonPropertyName("contact_sales_url")]
        [OpenApiProperty(Nullable = true, Description = "The SaaS app publisher's subscription sales page URL")]
        public string? ContactSalesUrl { get; set; }

        [JsonProperty("contact_support_email")]
        [JsonPropertyName("contact_support_email")]
        [OpenApiProperty(Nullable = true, Description = "The SaaS app publisher's subscription support email address")]
        public string? ContactSupportEmail { get; set; }

        [JsonProperty("contact_support_url")]
        [JsonPropertyName("contact_support_url")]
        [OpenApiProperty(Nullable = true, Description = "The SaaS app publisher's subscription support page URL")]
        public string? ContactSupportUrl { get; set; }

        [JsonProperty("mona_base_storage_url")]
        [JsonPropertyName("mona_base_storage_url")]
        [OpenApiProperty(Nullable = true, Description = "If Mona (https://github.com/microsoft/mona-saas) integration is configured, Mona's base storage URL for retrieving subscription information")]
        public string? MonaBaseStorageUrl { get; set; }

        [JsonProperty("mona_subscription_state")]
        [JsonPropertyName("mona_subscription_state")]
        [OpenApiProperty(Nullable = true, Description = "If Mona (https://github.com/microsoft/mona-saas) integration is configured, the default state that subscriptions should be created in when forwarded from Mona")]
        public string? DefaultMonaSubscriptionState { get; set; }

        [JsonProperty("mona_subscription_is_being_configured")]
        [JsonPropertyName("mona_subscription_is_being_configured")]
        [OpenApiProperty(Nullable = true, Description = "If Mona (https://github.com/microsoft/mona-saas) integration is configured, whether or not the subscription forwaded from Mona is currently being configured")]
        public bool MonaSubscriptionIsBeingConfigured { get; set; } = false;

        [JsonProperty("is_setup_complete")]
        [JsonPropertyName("is_setup_complete")]
        [OpenApiProperty(Nullable = false, Description = "Whether or not this Turnstile has been set up by the publisher")]
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
