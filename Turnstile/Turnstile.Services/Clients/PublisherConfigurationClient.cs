using Newtonsoft.Json;
using System.Net;
using Turnstile.Core.Extensions;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models.Configuration;

namespace Turnstile.Services.Clients
{
    public class PublisherConfigurationClient : IPublisherConfigurationClient
    {
        private const string url = "api/saas/publisher/configuration";

        private readonly HttpClient httpClient;

        public PublisherConfigurationClient(IHttpClientFactory httpClientFactory) =>
            httpClient = httpClientFactory.CreateClient(HttpClientNames.TurnstileApi);

        public async Task<PublisherConfiguration?> GetConfiguration()
        {
            using (var apiRequest = new HttpRequestMessage(HttpMethod.Get, url))
            {
                var apiResponse = await httpClient.SendAsync(apiRequest);

                if (apiResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    return null; // Configuration not found...
                }

                apiResponse.EnsureSuccessStatusCode();

                var jsonString = await apiResponse.Content.ReadAsStringAsync();
                var publisherConfig = JsonConvert.DeserializeObject<PublisherConfiguration>(jsonString);

                return publisherConfig;
            }
        }

        public async Task UpdateConfiguration(PublisherConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

            var validationErrors = configuration.Validate().ToList();

            if (validationErrors.Any())
            {
                throw new ArgumentException($"If [{nameof(PublisherConfiguration.IsSetupComplete)}]... {validationErrors.ToParagraph()}", nameof(configuration));
            }

            using (var apiRequest = new HttpRequestMessage(HttpMethod.Put, url))
            {
                apiRequest.Content = new StringContent(JsonConvert.SerializeObject(configuration));

                var apiResponse = await httpClient.SendAsync(apiRequest);

                apiResponse.EnsureSuccessStatusCode();
            }
        }
    }
}
