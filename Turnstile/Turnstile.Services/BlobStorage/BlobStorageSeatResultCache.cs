using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;
using static System.Environment;

namespace Turnstile.Services.BlobStorage
{
    public class BlobStorageSeatResultCache : ISeatResultCache
    {
        private readonly BlobContainerClient containerClient;
        private readonly ILogger logger;

        public BlobStorageSeatResultCache(ILogger<BlobStorageSeatResultCache> logger)
        {
            const string StorageConnectionStringEnvName = "Turnstile_SeatResultCache_StorageConnectionString";
            const string StorageContainerNameEnvName = "Tursntile_SeatResultCache_StorageContainerName";

            var connectionString = GetEnvironmentVariable(StorageConnectionStringEnvName)
                ?? throw new InvalidOperationException($"[{StorageConnectionStringEnvName}] environment variable not configured.");

            var containerName = GetEnvironmentVariable(StorageContainerNameEnvName)
                ?? throw new InvalidOperationException($"[{StorageContainerNameEnvName}] environment variable not configured.");

            var serviceClient = new BlobServiceClient(connectionString);

            this.containerClient = serviceClient.GetBlobContainerClient(containerName);
            this.logger = logger;
        }

        public async Task<string> CacheSeatResult(SeatResult seatResult)
        {
            ArgumentNullException.ThrowIfNull(seatResult, nameof(seatResult));

            try
            {
                var blobClient = containerClient.GetBlobClient(seatResult.RequestId);
                var expiryTime = DateTime.UtcNow.AddMinutes(5); // SAS token is only valid for 5 minutes for security reasons.

                await blobClient.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(seatResult))));

                var sasBuilder = new BlobSasBuilder(BlobContainerSasPermissions.Read, expiryTime)
                {
                    BlobContainerName = containerClient.Name,
                    BlobName = blobClient.Name,
                    Resource = "b"
                };

                var sasToken = blobClient.GenerateSasUri(sasBuilder).ToString();

                return sasToken.Substring(sasToken.IndexOf($"/{containerClient.Name.ToLower()}"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    $"An error occurred while attempting to put seat result [{seatResult.RequestId}] into blob storage." +
                    $"For more details, see exception: [{ex.Message}].");

                throw;
            }
        }
    }
}
