using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Turnstile.Core.Models.Profiles
{
    public class DeploymentProfileApp
    {
        [JsonProperty("isDeployed")]
        [JsonPropertyName("isDeployed")]
        public bool IsDeployed { get; set; }

        [JsonProperty("aadClientId")]
        [JsonPropertyName("aadClientId")]
        public string? EntraAppClientId { get; set; }

        [JsonProperty("name")]
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonProperty("baseUrl")]
        [JsonPropertyName("baseUrl")]
        public string? BaseUrl { get; set; }
    }
}
