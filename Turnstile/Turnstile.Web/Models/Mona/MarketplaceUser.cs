using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Turnstile.Web.Models.Mona
{
    public class MarketplaceUser
    {
        [JsonProperty("userId")]
        [JsonPropertyName("userId")]
        public string? UserId { get; set; }

        [JsonProperty("userEmail")]
        [JsonPropertyName("userEmail")]
        public string? UserEmail { get; set; }

        [JsonProperty("aadObjectId")]
        [JsonPropertyName("aadObjectId")]
        public string? AadObjectId { get; set; }

        [JsonProperty("aadTenantId")]
        [JsonPropertyName("aadTenantId")]
        public string? AadTenantId { get; set; }
    }
}
