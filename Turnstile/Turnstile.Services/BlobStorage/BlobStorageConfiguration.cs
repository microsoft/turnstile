using System.Text.Json.Serialization;
using static Turnstile.Core.TurnstileEnvironment;

namespace Turnstile.Services.BlobStorage
{
    public class BlobStorageConfiguration
    {
        [JsonPropertyName("storage_account_name")]
        public string? StorageAccountName { get; set; }

        [JsonPropertyName("container_name")]
        public string? ContainerName { get; set; }

        public static BlobStorageConfiguration FromPublisherConfigurationStorageEnvironmentVariables() =>
            new BlobStorageConfiguration
            {
                StorageAccountName = GetRequiredEnvironmentVariable(EnvironmentVariableNames.PublisherConfig.StorageAccountName),
                ContainerName = GetRequiredEnvironmentVariable(EnvironmentVariableNames.PublisherConfig.StorageContainerName)
            };
    }
}
