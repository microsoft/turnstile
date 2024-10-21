using Azure.Core;
using Azure.Identity;
using System.Net.Http.Headers;
using static Turnstile.Core.TurnstileEnvironment;

namespace Turnstile.Services.Extensions
{
    public static class HttpClientExtensions
    {
        public static HttpClient ConfigureForTurnstileApiAccess(this HttpClient httpClient)
        {
            ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));

            var apiBaseUrl = GetRequiredEnvironmentVariable(EnvironmentVariableNames.Api.BaseUrl);
            var apiScope = GetRequiredEnvironmentVariable(EnvironmentVariableNames.Api.AuthScope);
            var tokenRequestContext = new TokenRequestContext(new[] { apiScope });
            var tokenCredential = new DefaultAzureCredential();
            var tokenResponse = tokenCredential.GetToken(tokenRequestContext);

            httpClient.BaseAddress = new Uri(apiBaseUrl);

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.Token);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Turnstile", "1.0"));

            return httpClient;
        }
    }
}
