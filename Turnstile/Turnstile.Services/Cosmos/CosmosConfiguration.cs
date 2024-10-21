// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using static Turnstile.Core.TurnstileEnvironment;

namespace Turnstile.Services.Cosmos
{
    public class CosmosConfiguration
    {
        [JsonPropertyName("endpoint_url")]
        public string? EndpointUrl { get; set; }

        [JsonPropertyName("database_id")]
        public string? DatabaseId { get; set; }

        [JsonPropertyName("container_id")]
        public string? ContainerId { get; set; }

        public static CosmosConfiguration FromEnvironmentVariables() =>
            new CosmosConfiguration
            {
                EndpointUrl = GetRequiredEnvironmentVariable(EnvironmentVariableNames.Cosmos.EndpointUrl),
                DatabaseId = GetRequiredEnvironmentVariable(EnvironmentVariableNames.Cosmos.DatabaseId),
                ContainerId = GetRequiredEnvironmentVariable(EnvironmentVariableNames.Cosmos.ContainerId)
            };
    }
}
