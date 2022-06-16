// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
        public static IServiceCollection AddApiClients(this IServiceCollection services)
        {
            services.AddHttpClient(HttpClientNames.TurnstileApi, ConfigureApiClient)
                    .AddTransientHttpErrorPolicy(pb => pb.WaitAndRetryAsync(3, r => TimeSpan.FromMilliseconds(600)));

            // This is kind of gnarly but it wasn't really easy to use IHttpClientFactory and ILogger<T> within an Azure Function.
            // We can do it but it requires wiring up dependency injection within the function app and we're trying to keep this as simple
            // as possible. I thought about creating two constructors - one for the function app that takes in an HttpClient and an ILogger 
            // and another for the web app that takes IHttpClientFactory and ILogger<T> but, after reading this article -
            //
            // https://blogs.cuttingedge.it/steven/posts/2013/di-anti-pattern-multiple-constructors/
            //
            // I decided to just use the generic HttpClient and ILogger constructors for clients across the board. BUT, even though our 
            // constructors no longer take IHttpClientFactories and ILogger<T>s, there's still a lot of value in their implementation here 
            // within the web app (see https://docs.microsoft.com/aspnet/core/fundamentals/http-requests, https://docs.microsoft.com/aspnet/core/fundamentals/logging).
            // So, instead, we just use these simple inline factory methods that resolve an IHttpClientFactory to create the HttpClient to inject
            // into the constructor and ILogger<T> which is passed in as ILogger (we still retain the log category functionality that comes from ILogger<T> -
            // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-6.0#log-category).

            services.AddScoped<IPublisherConfigurationClient, PublisherConfigurationClient>(p =>
                new PublisherConfigurationClient(
                    p.GetService<IHttpClientFactory>()!,
                    p.GetService<ILogger<PublisherConfigurationClient>>()!));

            services.AddScoped<ISeatsClient, SeatsClient>(p =>
                new SeatsClient(
                    p.GetService<IHttpClientFactory>()!.CreateClient(),
                    p.GetService<ILogger<SeatsClient>>()!));

            services.AddScoped<ISubscriptionsClient, SubscriptionsClient>(p =>
                new SubscriptionsClient(
                    p.GetService<IHttpClientFactory>()!.CreateClient(),
                    p.GetService<ILogger<SubscriptionsClient>>()!));

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
