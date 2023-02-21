using Azure.Messaging.EventGrid;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using Turnstile.Core.Interfaces;
using static System.Environment;
using static Turnstile.Core.Constants.EnvironmentVariableNames;

namespace Turnstile.Api.Utilities
{
    public class CheckTurnstileHealth
    {
        private readonly ITurnstileRepository turnstileRepo;

        public CheckTurnstileHealth(ITurnstileRepository turnstileRepo) => this.turnstileRepo = turnstileRepo;

        [FunctionName("CheckTurnstileHealth")]
        public async Task<IActionResult> RunCheckTurnstileHealth(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req,
            [Blob("turn-configuration/publisher_config.json", FileAccess.Read, Connection = Storage.StorageConnectionString)] string publisherConfigJson,
            ILogger log)
        {
            try
            {
                log.LogDebug("Checking health...");

                if (CanAccessStorageAccount(log) &&
                    await CanAccessEventGrid(log) &&
                    await CanAccessTurnstileRepository(log))
                {
                    log.LogDebug("Turnstile is healthy.");

                    return new OkResult();
                }
                else
                {
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }
            }
            catch (Exception ex)
            {
                // Something's really broken here. We couldn't even get through the health check.

                log.LogError($"An error occurred while checking Turnstile health: [{ex.Message}].");

                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        private async Task<bool> CanAccessEventGrid(ILogger log)
        {
            try
            {
                // Try to send a test message to the event grid topic

                var eventGridClient = new EventGridPublisherClient(
                    new Uri(GetEnvironmentVariable(EventGrid.EndpointUrl)
                        ?? throw new InvalidOperationException($"[{EventGrid.EndpointUrl}] environment variable not configured.")),
                    new Azure.AzureKeyCredential(GetEnvironmentVariable(EventGrid.AccessKey)
                        ?? throw new InvalidOperationException($"[{EventGrid.AccessKey}] environment variable not configured.")));

                var healthEvent = new EventGridEvent(
                    Guid.NewGuid().ToString(),
                    "turnstile_health_check",
                    "turnstile_health_check",
                    new { Message = "Please ignore. This is an automated health check event." });

                await eventGridClient.SendEventAsync(healthEvent);

                return true;
            }
            catch (Exception ex)
            {
                log.LogWarning($"Event grid connection health check failed: [{ex.Message}].");

                return false;
            }
        }

        private bool CanAccessStorageAccount(ILogger log)
        {
            // Check to see if the turn-configuration blob container is there

            try
            {
                var storageConnString =
                    GetEnvironmentVariable(Storage.StorageConnectionString)
                    ?? throw new InvalidOperationException($"[{Storage.StorageConnectionString}] environment variable not configured.");

                var serviceClient = new BlobServiceClient(storageConnString);
                var containerClient = serviceClient.GetBlobContainerClient("turn-configuration");

                if (containerClient.Exists().Value)
                {
                    return true;
                }
                else
                {
                    log.LogWarning($"Publisher configuration blob not found. Health check failed.");

                    return false;
                }
            }
            catch (Exception ex)
            {
                log.LogWarning($"Storage account connection health check failed: [{ex.Message}].");

                return false;
            }
        }

        private async Task<bool> CanAccessTurnstileRepository(ILogger log)
        {
            try
            {
                // Try to get a subscription that we know doesn't exist.
                // Should't throw an exception (it just returns null).

                await turnstileRepo.GetSubscription(Guid.NewGuid().ToString());

                return true;
            }
            catch (Exception ex)
            {
                log.LogWarning($"Turnstile repository health check failed: [{ex.Message}].");

                return false;
            }
        }
    }
}
