using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Newtonsoft.Json;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models.Profiles;

namespace Turnstile.Services.BlobStorage
{
    public class BlobStorageDeploymentProfileStore : IDeploymentProfileStore
    {
        private const string blobName = "deployment/standard-v1/profile.json";

        private readonly BlobClient deploymentProfileBlob;

        public BlobStorageDeploymentProfileStore(BlobStorageConfiguration blobStorageConfig)
        {
            ArgumentNullException.ThrowIfNull(blobStorageConfig, nameof(blobStorageConfig));

            var serviceClient = new BlobServiceClient(
                new Uri($"https://{blobStorageConfig.StorageAccountName}.blob.core.windows.net"),
                new DefaultAzureCredential());

            var containerClient =
                serviceClient.GetBlobContainerClient(blobStorageConfig.ContainerName);

            deploymentProfileBlob = containerClient.GetBlobClient(blobName);
        }

        public async Task<DeploymentProfile?> GetDeploymentProfile()
        {
            if (await deploymentProfileBlob.ExistsAsync())
            {
                BlobDownloadResult download = await deploymentProfileBlob.DownloadContentAsync();
                return JsonConvert.DeserializeObject<DeploymentProfile>(download.Content.ToString());
            }
            else
            {
                return null;
            }
        }
    }
}
