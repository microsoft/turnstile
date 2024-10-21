using System.Text.Json.Serialization;
using static Turnstile.Core.TurnstileEnvironment;

namespace Turnstile.Services.EventGrid
{
    public class EventGridConfiguration
    {
        [JsonPropertyName("topic_endpoint")]
        public string? TopicEndpoint { get; set; }

        public static EventGridConfiguration FromEnvironmentVariables() =>
            new EventGridConfiguration
            {
                TopicEndpoint = GetRequiredEnvironmentVariable(EnvironmentVariableNames.EventGrid.TopicEndpointUrl)
            };
    }
}
