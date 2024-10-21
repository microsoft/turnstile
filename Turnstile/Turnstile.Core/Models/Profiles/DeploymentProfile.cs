using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Turnstile.Core.Models.Profiles
{
    public class DeploymentProfile
    {
        [JsonProperty("isHeadless")]
        [JsonPropertyName("isHeadless")]
        public bool IsHeadless { get; set; }

        [JsonProperty("deploymentName")]
        [JsonPropertyName("deploymentName")]
        public string? DeploymentName { get; set; }

        [JsonProperty("deployedVersion")]
        [JsonPropertyName("deployedVersion")]
        public string? DeployedVersion { get; set; }

        [JsonProperty("azureDeploymentName")]
        [JsonPropertyName("azureDeploymentName")]
        public string? AzureDeploymentName { get; set; }

        [JsonProperty("azureSubscriptionId")]
        [JsonPropertyName("azureSubscriptionId")]
        public string? AzureSubscriptionId { get; set; }

        [JsonProperty("azureResourceGroupName")]
        [JsonPropertyName("azureResourceGroupName")]
        public string? AzureResourceGroupName { get; set; }

        [JsonProperty("azureRegion")]
        [JsonPropertyName("azureRegion")]
        public string? AzureRegion { get; set; }

        [JsonProperty("eventGridTopicName")]
        [JsonPropertyName("eventGridTopicName")]
        public string? EventGridTopicName { get; set; }

        [JsonProperty("aadTenantId")]
        [JsonPropertyName("aadTenantId")]
        public string? EntraTenantId { get; set; }

        [JsonProperty("apps")]
        [JsonPropertyName("apps")]
        public DeploymentProfileApps? Apps { get; set; }
    }
}
