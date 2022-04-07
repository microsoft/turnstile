using Newtonsoft.Json;
using System.Net;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;

namespace Turnstile.Services.Clients
{
    public class SubscriptionsClient : ISubscriptionsClient
    {
        private readonly HttpClient httpClient;

        public SubscriptionsClient(IHttpClientFactory httpClientFactory) =>
            httpClient = httpClientFactory.CreateClient(HttpClientNames.TurnstileApi);

        public async Task<Subscription?> CreateSubscription(Subscription subscription)
        {
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));

            var url = $"api/saas/subscriptions/{subscription.SubscriptionId}";

            using (var apiRequest = new HttpRequestMessage(HttpMethod.Post, url))
            {
                apiRequest.Content = new StringContent(JsonConvert.SerializeObject(subscription));

                var apiResponse = await httpClient.SendAsync(apiRequest);

                var actualError = await apiResponse.Content.ReadAsStringAsync();

                apiResponse.EnsureSuccessStatusCode();

                var jsonString = await apiResponse.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<Subscription>(jsonString);
            }
        }

        public async Task<Subscription?> GetSubscription(string subscriptionId)
        {
            ArgumentNullException.ThrowIfNull(subscriptionId, nameof(subscriptionId));

            var url = $"api/saas/subscriptions/{subscriptionId}";

            using (var apiRequest = new HttpRequestMessage(HttpMethod.Get, url))
            {
                var apiResponse = await httpClient.SendAsync(apiRequest);

                if (apiResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    apiResponse.EnsureSuccessStatusCode();

                    var jsonString = await apiResponse.Content.ReadAsStringAsync();

                    return JsonConvert.DeserializeObject<Subscription>(jsonString);
                }
            }
        }

        public async Task<IEnumerable<Subscription>> GetSubscriptions(string? tenantId = null)
        {
            var url = "api/saas/subscriptions";

            if (!string.IsNullOrEmpty(tenantId))
            {
                url += $"?tenant_id={tenantId}";
            }

            using (var apiRequest = new HttpRequestMessage(HttpMethod.Get, url))
            {
                var apiResponse = await httpClient.SendAsync(apiRequest);

                apiResponse.EnsureSuccessStatusCode();

                var jsonString = await apiResponse.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<List<Subscription>>(jsonString)!;
            }
        }

        public async Task<Subscription?> UpdateSubscription(Subscription subscription)
        {
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));

            var url = $"api/saas/subscriptions/{subscription.SubscriptionId}";

            using (var apiRequest = new HttpRequestMessage(HttpMethod.Patch, url))
            {
                apiRequest.Content = new StringContent(JsonConvert.SerializeObject(subscription));

                var apiResponse = await httpClient.SendAsync(apiRequest);

                apiResponse.EnsureSuccessStatusCode();

                var jsonString = await apiResponse.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<Subscription>(jsonString);
            }
        }
    }
}
