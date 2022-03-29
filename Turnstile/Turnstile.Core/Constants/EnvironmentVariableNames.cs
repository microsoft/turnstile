namespace Turnstile.Core.Constants
{
    public static class EnvironmentVariableNames
    {
        public static class Cosmos
        {
            public const string EndpointUrl = "Turnstile_CosmosEndpointUrl";
            public const string AccessKey = "Turnstile_CosmosAccessKey";
            public const string DatabaseId = "Turnstile_CosmosDatabaseId";
            public const string ContainerId = "Turnstile_CosmosContainerId";
        }

        public static class EventGrid
        {
            public const string EndpointUrl = "Turnstile_EventGridEndpointUrl";
            public const string AccessKey = "Turnstile_EventGridAccessKey";
        }

        public static class ApiAccess
        {
            public const string BaseUrl = "Turnstile_ApiBaseUrl";
            public const string AccessKey = "Turnstile_ApiAccessKey";
        }

        public static class Mona
        {
            public const string BaseStorageUrl = "Turnstile_MonaBaseStorageUrl";
        }

        public static class Storage
        {
            public const string StorageConnectionString = "Turnstile_PublisherConfigStorageConnectionString";
            public const string StorageContainerName = "Turnstile_PublisherConfigStorageContainerName";
            public const string StorageBlobName = "Turnstile_PublisherConfigStorageBlobName";
        }
    }
}
