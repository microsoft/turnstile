using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Turnstile.Core.Models
{
    public class SubscriberInfo
    {
        [JsonProperty("tenant_name")]
        [JsonPropertyName("tenant_name")]
        public string? TenantName { get; set; }

        [JsonProperty("tenant_country")]
        [JsonPropertyName("tenant_country")]
        public string? TenantCountry { get; set; }

        [JsonProperty("admin_name")]
        [JsonPropertyName("admin_name")]
        public string? AdminName { get; set; }

        [JsonProperty("admin_email")]
        [JsonPropertyName("admin_email")]
        public string? AdminEmail { get; set; }
    }
}
