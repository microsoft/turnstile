using Microsoft.Net.Http.Headers;
using static System.Environment;
using static Turnstile.Core.Constants.EnvironmentVariableNames;

namespace Turnstile.Web.Extensions
{
    public static class ServiceCollectionExtensions
    {
        private static void ConfigureInternalApiClient(HttpClient client)
        {
            var apiKey = GetEnvironmentVariable(ApiAccess.AccessKey);
            var apiBaseUrl = GetEnvironmentVariable(ApiAccess.BaseUrl);

            if (string.IsNullOrEmpty(apiBaseUrl))
            {
                throw new Exception($"Unable to create API client. [{ApiAccess.BaseUrl}] environment variable not configured.");
            }

            client.BaseAddress = new Uri(apiBaseUrl);

            client.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
            client.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "Turnstile.Web");

            if (!string.IsNullOrEmpty(apiKey))
            {
                client.DefaultRequestHeaders.Add("x-functions-key", apiKey);
            }
        }
    }
}
