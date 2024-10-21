namespace Turnstile.Core
{
    public static class TurnstileEnvironment
    {
        public static string GetRequiredEnvironmentVariable(string name) =>
            Environment.GetEnvironmentVariable(name ?? throw new ArgumentNullException(nameof(name)))
            ?? throw new InvalidOperationException($"[{name}] environment variable not configured.");

        public static class EnvironmentVariableNames
        {
            public static class Api
            {
                public const string AuthAudience = "Turnstile_ApiAuthAudience";
                public const string AuthScope = "Turnstile_ApiAuthScope";
                public const string AuthTenantId = "Turnstile_ApiAuthTenantId";
                public const string BaseUrl = "Turnstile_ApiBaseUrl";
            }

            public static class Cosmos
            {
                public const string ContainerId = "Turnstile_CosmosContainerId";
                public const string DatabaseId = "Turnstile_CosmosDatabaseId";
                public const string EndpointUrl = "Turnstile_CosmosEndpointUrl";
            }

            public static class EventGrid
            {
                public const string TopicEndpointUrl = "Turnstile_EventGridTopicEndpointUrl";
            }

            public static class SeatResultCache
            {
                public const string StorageAccountName = "Turnstile_SeatResultCache_StorageAccountName";
                public const string StorageBlobName = "Turnstile_SeatResultCache_StorageBlobName";
                public const string StorageContainerName = "Turnstile_SeatResultCache_StorageContainerName";
            }

            public static class PublisherConfig
            {
                public const string StorageAccountName = "Turnstile_PublisherConfig_StorageAccountName";
                public const string StorageBlobName = "Turnstile_PublisherConfig_StorageBlobName";
                public const string StorageContainerName = "Turnstile_PublisherConfig_StorageContainerName";
            }
        }
    }
}
