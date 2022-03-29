using Newtonsoft.Json;

namespace Turnstile.Core.Models.Configuration
{
    public class PublisherConfiguration
    {
        [JsonProperty("turnstile_name")]
        public string? TurnstileName { get; set; }

        [JsonProperty("publisher_name")]
        public string? PublisherName { get; set; }

        [JsonProperty("home_page_url")]
        public string? HomePageUrl { get; set; }

        [JsonProperty("contact_page_url")]
        public string? ContactPageUrl { get; set; }

        [JsonProperty("privacy_notice_page_url")]
        public string? PrivateNoticePageUrl { get; set; }

        [JsonProperty("is_setup_complete")]
        public bool IsSetupComplete { get; set; } = false;

        [JsonProperty("default_seating_config")]
        public SeatingConfiguration? SeatingConfiguration { get; set; }

        [JsonProperty("turnstile_config")]
        public TurnstileConfiguration? TurnstileConfiguration { get; set; }
    }
}
