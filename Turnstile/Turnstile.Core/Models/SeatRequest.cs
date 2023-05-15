using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Turnstile.Core.Models
{
    public class SeatRequest
    {
        [JsonProperty("request_id")]
        [JsonPropertyName("request_id")]
        [OpenApiProperty(Nullable = true, Description = "Unique seat request identifier")]
        public string? RequestId { get; set; }

        [JsonProperty("user_id")]
        [JsonPropertyName("user_id")]
        [OpenApiProperty(Nullable = false, Description = "User (ID) that a seat is being requested for")]
        public string? UserId { get; set; }

        [JsonProperty("tenant_id")]
        [JsonPropertyName("tenant_id")]
        [OpenApiProperty(Nullable = false, Description = "User (tenant ID) that a seat is being requested for")]
        public string? TenantId { get; set; }

        [JsonProperty("user_name")]
        [JsonPropertyName("user_name")]
        [OpenApiProperty(Nullable = true, Description = "User (name) that a seat is being requested for")]
        public string? UserName { get; set; }

        [JsonProperty("emails")]
        [JsonPropertyName("emails")]
        [OpenApiProperty(Nullable = true, Description = "Email addresses associated with the user this seat is being requested for")]
        public List<string> EmailAddresses { get; set; } = new List<string>();

        [JsonProperty("roles")]
        [JsonPropertyName("roles")]
        [OpenApiProperty(Nullable = true, Description = "Roles that the user this seat is being requested for belong to")]
        public List<string> Roles { get; set; } = new List<string>();
    }
}
