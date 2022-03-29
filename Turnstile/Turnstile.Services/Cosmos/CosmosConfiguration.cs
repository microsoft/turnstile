using System.Text.Json.Serialization;
using Turnstile.Core.Constants;
using static System.Environment;

namespace Turnstile.Services.Cosmos
{
    public class CosmosConfiguration
    {
        [JsonPropertyName("endpoint_url")]
        public string? EndpointUrl { get; set; }

        [JsonPropertyName("access_key")]
        public string? AccessKey { get; set; }

        [JsonPropertyName("database_id")]
        public string? DatabaseId { get; set; }

        [JsonPropertyName("container_id")]
        public string? ContainerId { get; set; }

        public static CosmosConfiguration FromEnvironmentVariables() =>
            new CosmosConfiguration
            {
                EndpointUrl = GetEnvironmentVariable(EnvironmentVariableNames.Cosmos.EndpointUrl),
                AccessKey = GetEnvironmentVariable(EnvironmentVariableNames.Cosmos.AccessKey),
                DatabaseId = GetEnvironmentVariable(EnvironmentVariableNames.Cosmos.DatabaseId),
                ContainerId = GetEnvironmentVariable(EnvironmentVariableNames.Cosmos.ContainerId)
            };
    }
}
