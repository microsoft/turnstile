// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Identity;
using Azure.Messaging.EventGrid;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Turnstile.Core.Interfaces;
using static Turnstile.Core.TurnstileEnvironment;

namespace Turnstile.Api.Utilities
{
    public class CheckTurnstileHealth
    {
        private readonly ILogger log;
        private readonly ISubscriptionEventPublisher eventPublisher;
        private readonly ITurnstileRepository turnstileRepo;

        public CheckTurnstileHealth(
            ILogger<CheckTurnstileHealth> log,
            ISubscriptionEventPublisher eventPublisher,
            ITurnstileRepository turnstileRepo)
        {
            this.log = log;
            this.eventPublisher = eventPublisher;
            this.turnstileRepo = turnstileRepo;
        }

        [Function("CheckTurnstileHealth")]
        public async Task<IActionResult> RunCheckTurnstileHealth(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogDebug("Checking health...");

                if (CanAccessStorageAccount() &&
                    await CanAccessEventGrid() &&
                    await CanAccessTurnstileRepository())
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

        private async Task<bool> CanAccessEventGrid()
        {
            try
            {
                // Try to send a test message to the event grid topic

                var eventGridClient = new EventGridPublisherClient(
                    new Uri(GetRequiredEnvironmentVariable(EnvironmentVariableNames.EventGrid.TopicEndpointUrl)),
                    new DefaultAzureCredential());

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

        private bool CanAccessStorageAccount()
        {
            // Check to see if the turn-configuration blob container is there

            try
            {
                var storageAccountName =
                    GetRequiredEnvironmentVariable(EnvironmentVariableNames.PublisherConfig.StorageAccountName);

                var serviceClient = new BlobServiceClient(
                    new Uri($"https://{storageAccountName}.blob.core.windows.net"),
                    new DefaultAzureCredential());

                var containerClient = serviceClient.GetBlobContainerClient("turn-configuration");

                if (containerClient.Exists().Value)
                {
                    return true;
                }
                else
                {
                    log.LogWarning($"Publisher configuration container not found. Health check failed.");

                    return false;
                }
            }
            catch (Exception ex)
            {
                log.LogWarning($"Storage account connection health check failed: [{ex.Message}].");

                return false;
            }
        }

        private async Task<bool> CanAccessTurnstileRepository()
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
