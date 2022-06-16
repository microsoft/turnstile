using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Turnstile.Core.Models
{
    public class SeatRequest
    {
        [JsonProperty("request_id")]
        [JsonPropertyName("request_id")]
        public string? RequestId { get; set; }

        [JsonProperty("user_id")]
        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }

        [JsonProperty("tenant_id")]
        [JsonPropertyName("tenant_id")]
        public string? TenantId { get; set; }

        [JsonProperty("emails")]
        [JsonPropertyName("emails")]
        public List<string> EmailAddresses { get; set; } = new List<string>();

        [JsonProperty("roles")]
        [JsonPropertyName("roles")]
        public List<string> Roles { get; set; } = new List<string>();
    }
}
