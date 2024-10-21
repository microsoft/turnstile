using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Turnstile.Core.Models.Profiles
{
    public class DeploymentProfileApps
    {
        [JsonProperty("api")]
        [JsonPropertyName("api")]
        public DeploymentProfileApp? ApiApp { get; set; }

        [JsonProperty("userWeb")]
        [JsonPropertyName("userWeb")]
        public DeploymentProfileApp? UserWebApp { get; set; }

        [JsonProperty("adminWeb")]
        [JsonPropertyName("adminWeb")]
        public DeploymentProfileApp? AdminWebApp { get; set; }
    }
}
