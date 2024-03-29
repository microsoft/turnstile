// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Messaging.EventGrid;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Turnstile.Core.Constants;
using static System.Environment;

namespace Turnstile.Api.Testing
{
    public class PostEventToStore
    {
        [FunctionName("PostEventToStore")]
        public async Task RunPostEventToStore([EventGridTrigger]EventGridEvent eventGridEvent, ILogger log)
        {
            // This is just an event sink that dumps Turnstile events to blob storage. It's not connected by default 
            // and is normally only connected when the end-to-end test script (./test/e2e.sh) runs. The script checks the events dumped
            // to blob storage to verify that all of the expected events were fired.

            var eventName = $"{eventGridEvent.Subject}/{eventGridEvent.EventType}/{eventGridEvent.Id}";

            try
            {
                var storageConnectionString =
                    GetEnvironmentVariable(EnvironmentVariableNames.Testing.EventStorageConnectionString)
                    ?? throw new InvalidOperationException($"[{EnvironmentVariableNames.Testing.EventStorageConnectionString}] not configured.");

                var containerClient = new BlobContainerClient(storageConnectionString, "event-store");
                var blobClient = containerClient.GetBlobClient(eventName);

                log.LogDebug($"Saving event [{eventName}] to event store...");

                await blobClient.UploadAsync(BinaryData.FromString(eventGridEvent.Data.ToString()));

                log.LogDebug($"Saved event [{eventName}] to event store.");
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"An error occurred while trying to save event [{eventName}] to event store. See exception for details.");
            }
        }
    }
}
