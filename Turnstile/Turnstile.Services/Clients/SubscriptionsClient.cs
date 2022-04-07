using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;

namespace Turnstile.Services.Clients
{
    public class SubscriptionsClient : ISubscriptionsClient
    {
        private readonly HttpClient httpClient;
        private readonly ILogger logger;

        public SubscriptionsClient(IHttpClientFactory httpClientFactory, ILogger<SubscriptionsClient> logger)
        {
            httpClient = httpClientFactory.CreateClient(HttpClientNames.TurnstileApi);
            this.logger = logger;
        }

        public async Task<Subscription?> CreateSubscription(Subscription subscription)
        {
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));

            var url = $"api/saas/subscriptions/{subscription.SubscriptionId}";

            using (var apiRequest = new HttpRequestMessage(HttpMethod.Post, url))
            {
                apiRequest.Content = new StringContent(JsonConvert.SerializeObject(subscription));

                var apiResponse = await httpClient.SendAsync(apiRequest);

                if (apiResponse.IsSuccessStatusCode)
                {
                    var jsonString = await apiResponse.Content.ReadAsStringAsync();

                    return JsonConvert.DeserializeObject<Subscription>(jsonString);
                }
                else
                {
                    var apiError = await apiResponse.Content.ReadAsStringAsync();
                    var errorMessage = $"Turnstile API POST [{url}] failed with status code [{apiResponse.StatusCode}]: [{apiError}]";

                    logger.LogError(errorMessage);

                    throw new HttpRequestException(errorMessage);
                }
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
                else if (apiResponse.IsSuccessStatusCode)
                {
                    var jsonString = await apiResponse.Content.ReadAsStringAsync();

                    return JsonConvert.DeserializeObject<Subscription>(jsonString);
                }
                else
                {
                    var apiError = await apiResponse.Content.ReadAsStringAsync();
                    var errorMessage = $"Turnstile API GET [{url}] failed with status code [{apiResponse.StatusCode}]: [{apiError}]";

                    logger.LogError(errorMessage);

                    throw new HttpRequestException(errorMessage);
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

                if (apiResponse.IsSuccessStatusCode)
                {
                    var jsonString = await apiResponse.Content.ReadAsStringAsync();

                    return JsonConvert.DeserializeObject<List<Subscription>>(jsonString)!;
                }
                else
                {
                    var apiError = await apiResponse.Content.ReadAsStringAsync();
                    var errorMessage = $"Turnstile API GET [{url}] failed with status code [{apiResponse.StatusCode}]: [{apiError}]";

                    logger.LogError(errorMessage);

                    throw new HttpRequestException(errorMessage);
                }
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

                if (apiResponse.IsSuccessStatusCode)
                {
                    var jsonString = await apiResponse.Content.ReadAsStringAsync();

                    return JsonConvert.DeserializeObject<Subscription>(jsonString);
                }
                else
                {
                    var apiError = await apiResponse.Content.ReadAsStringAsync();
                    var errorMessage = $"Turnstile API PATCH [{url}] failed with status code [{apiResponse.StatusCode}]: [{apiError}]";

                    logger.LogError(errorMessage);

                    throw new HttpRequestException(errorMessage);
                }
            }
        }
    }
}
