using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Turnstile.Core.Models.Configuration
{
    public class ClaimsConfiguration
    {
        [JsonProperty("user_id_claims")]
        [JsonPropertyName("user_id_claims")]
        public string[]? UserIdClaimTypes { get; set; }

        [JsonProperty("user_name_claims")]
        [JsonPropertyName("user_name_claims")]
        public string[]? UserNameClaimTypes { get; set; }

        [JsonProperty("tenant_id_claims")]
        [JsonPropertyName("tenant_id_claims")]
        public string[]? TenantIdClaimTypes { get; set; }

        [JsonProperty("email_claims")]
        [JsonPropertyName("email_claims")]
        public string[]? EmailClaimTypes { get; set; }

        [JsonProperty("role_claims")]
        [JsonPropertyName("role_claims")]
        public string[]? RoleClaimTypes { get; set; }

        public IEnumerable<string> Validate()
        {
            if (UserIdClaimTypes?.Any() != true)
            {
                yield return "[user_id_claims] are required.";
            }

            if (UserNameClaimTypes?.Any() != true)
            {
                yield return "[user_name_claims] are required.";
            }

            if (TenantIdClaimTypes?.Any() != true)
            {
                yield return "[tenant_id_claims] are required.";
            }

            if (EmailClaimTypes?.Any() != true)
            {
                yield return "[email_claims] are required.";
            }

            if (RoleClaimTypes?.Any() != true)
            {
                yield return "[role_claims] are required.";
            }
        }
    }
}
