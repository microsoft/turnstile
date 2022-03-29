using Newtonsoft.Json;
using System.Text.Json.Serialization;

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
    }
}
