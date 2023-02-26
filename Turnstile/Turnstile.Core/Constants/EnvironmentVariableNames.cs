// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Turnstile.Core.Constants;

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
        public const string EndpointUrl = "Turnstile_EventGridTopicEndpointUrl";
        public const string AccessKey = "Turnstile_EventGridTopicAccessKey";
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
    }

    public static class Testing
    {
        public const string EventStorageConnectionString = "Turnstile_EventStorageConnectionString";
    }

    public static class Publisher
    {
        public const string TenantId = "Turnstile_PublisherTenantId";
        public const string AdminRoleName = "Turnstile_PublisherAdminRoleName";
    }

    public static class Subscriber
    {
        public const string TenantAdminRoleName = "Turnstile_SubscriberTenantAdminRoleName";
    }
}

