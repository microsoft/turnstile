using System.Net;
using System.Text.Json;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models.Configuration;

namespace Turnstile.Services.Clients
{
    public class PublisherConfigurationClient : IPublisherConfigurationClient
    {
        private readonly HttpClient httpClient;

        public PublisherConfigurationClient(HttpClient httpClient) =>
            this.httpClient = httpClient;

        public async Task<PublisherConfiguration?> GetConfiguration()
        {
            var url = "api/saas/publisher/configuration";

            using (var apiRequest = new HttpRequestMessage(HttpMethod.Get, url))
            {
                var apiResponse = await httpClient.SendAsync(apiRequest);

                if (apiResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    return null; // Configuration not found...
                }

                apiResponse.EnsureSuccessStatusCode();

                var jsonString = await apiResponse.Content.ReadAsStringAsync();
                var seat = JsonSerializer.Deserialize<PublisherConfiguration>(jsonString);

                return seat;
            }
        }

        public async Task UpdateConfiguration(PublisherConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

            // TODO: Validate publisher configuration here...

            var url = "api/saas/publisher/configuration";

            using (var apiRequest = new HttpRequestMessage(HttpMethod.Put, url))
            {
                apiRequest.Content = new StringContent(JsonSerializer.Serialize(configuration));

                var apiResponse = await httpClient.SendAsync(apiRequest);

                apiResponse.EnsureSuccessStatusCode();
            }
        }
    }
}
