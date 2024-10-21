using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Newtonsoft.Json;
using System.Text;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models.Configuration;

namespace Turnstile.Services.BlobStorage
{
    public class BlobStoragePublisherConfigurationStore : IPublisherConfigurationStore
    {
        private const string blobName = "publisher_config.json";

        private readonly BlobClient configBlob;

        public BlobStoragePublisherConfigurationStore(BlobStorageConfiguration blobStorageConfig)
        {
            ArgumentNullException.ThrowIfNull(blobStorageConfig, nameof(blobStorageConfig));

            var serviceClient = new BlobServiceClient(
                new Uri($"https://{blobStorageConfig.StorageAccountName}.blob.core.windows.net"),
                new DefaultAzureCredential());

            var containerClient =
                serviceClient.GetBlobContainerClient(blobStorageConfig.ContainerName);

            configBlob = containerClient.GetBlobClient(blobName);
        }

        public async Task<PublisherConfiguration?> GetConfiguration()
        {
            if (await configBlob.ExistsAsync())
            {
                BlobDownloadResult download = await configBlob.DownloadContentAsync();
                return JsonConvert.DeserializeObject<PublisherConfiguration>(download.Content.ToString());
            }
            else
            {
                return null;
            }
        }

        public async Task<PublisherConfiguration> PutConfiguration(PublisherConfiguration publisherConfig)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));

            var configStream = new MemoryStream(
                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(publisherConfig)));

            await configBlob.UploadAsync(configStream, overwrite: true);

            return publisherConfig;
        }
    }

}