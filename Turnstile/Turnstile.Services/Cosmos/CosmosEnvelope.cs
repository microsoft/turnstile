using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Turnstile.Services.Cosmos
{
    // Instead of having a bunch of Cosmos collections, let's start with just one. This little class
    // wraps all of the objects that we persist to Cosmos and gives us a clean common surface to dictate
    // whatever kind of partitioning scheme we want without having to recreate all the core models specifically for Cosmos.

    public class CosmosEnvelope<TData>
    {
        [JsonPropertyName("id")]
        [JsonProperty("id")] // This is a nasty ugly hack that I'm pretty unhappy about. It will have to be like this until Cosmos supports System.Text.Json...
        public string? Id { get; set; }

        [JsonPropertyName("partition_id")]
        [JsonProperty("partition_id")]
        public string? PartitionId { get; set; }

        [JsonPropertyName("data_type")]
        [JsonProperty("data_type")]
        public string? DataType { get; set; }

        [JsonPropertyName("_etag")]
        [JsonProperty("_etag", NullValueHandling = NullValueHandling.Ignore)]
        public string? Etag { get; set; }

        [JsonPropertyName("ttl")]
        [JsonProperty("ttl", NullValueHandling = NullValueHandling.Ignore)]
        public int? TimeToLive { get; set; }

        [JsonPropertyName("data")]
        [JsonProperty("data")]
        public TData? Data { get; set; }
    }
}
