using Microsoft.Net.Http.Headers;
using Polly;
using Turnstile.Core.Interfaces;
using Turnstile.Services.Clients;
using static System.Environment;
using static Turnstile.Core.Constants.EnvironmentVariableNames;

namespace Turnstile.Web.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IHttpClientBuilder AddApiClient<TClient>(this IServiceCollection services)
            where TClient : class =>
            services.AddHttpClient<TClient>(ConfigureApiClient)
                    .AddTransientHttpErrorPolicy(pb => pb.WaitAndRetryAsync(3, r => TimeSpan.FromMilliseconds(600)));

        public static IHttpClientBuilder AddApiClient<TClient, TImplementation>(this IServiceCollection services)
            where TClient : class
            where TImplementation : class, TClient =>
            services.AddHttpClient<TClient, TImplementation>(ConfigureApiClient)
                    .AddTransientHttpErrorPolicy(pb => pb.WaitAndRetryAsync(3, r => TimeSpan.FromMilliseconds(600)));

        public static IServiceCollection AddApiClients(this IServiceCollection services)
        {
            services.AddApiClient<PublisherConfigurationClient>();
            services.AddApiClient<SeatsClient>();
            services.AddApiClient<SubscriptionsClient>();

            services.AddScoped<IPublisherConfigurationClient, PublisherConfigurationClient>();
            services.AddScoped<ISeatsClient, SeatsClient>();
            services.AddScoped<ISubscriptionsClient, SubscriptionsClient>();

            return services;
        }

        private static void ConfigureApiClient(HttpClient client)
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
