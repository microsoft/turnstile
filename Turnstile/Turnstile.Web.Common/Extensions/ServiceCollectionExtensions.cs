// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Turnstile.Core.Interfaces;
using Turnstile.Services.Clients;
using Turnstile.Services.Extensions;

namespace Turnstile.Web.Common.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApiClients(this IServiceCollection services, string userAgent)
        {
            ArgumentNullException.ThrowIfNull(services, nameof(services));
            ArgumentNullException.ThrowIfNull(userAgent, nameof(userAgent));

            services.AddHttpClient(HttpClientNames.TurnstileApi, h => h.ConfigureForTurnstileApiAccess())
                    .AddTransientHttpErrorPolicy(pb => pb.WaitAndRetryAsync(3, r => TimeSpan.FromMilliseconds(600)));
             
            services.AddScoped<IPublisherConfigurationClient, PublisherConfigurationClient>(p =>
                new PublisherConfigurationClient(
                    p.GetService<IHttpClientFactory>()!,
                    p.GetService<ILogger<PublisherConfigurationClient>>()!));

            services.AddScoped<ISeatsClient, SeatsClient>(p =>
                new SeatsClient(
                    p.GetService<IHttpClientFactory>()!.CreateClient(HttpClientNames.TurnstileApi),
                    p.GetService<ILogger<SeatsClient>>()!));

            services.AddScoped<ISubscriptionsClient, SubscriptionsClient>(p =>
                new SubscriptionsClient(
                    p.GetService<IHttpClientFactory>()!.CreateClient(HttpClientNames.TurnstileApi),
                    p.GetService<ILogger<SubscriptionsClient>>()!));

            return services;
        }
    }
}
