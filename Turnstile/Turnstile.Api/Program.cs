using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Turnstile.Api.Interfaces;
using Turnstile.Api.Services;
using Turnstile.Core.Interfaces;
using Turnstile.Services.BlobStorage;
using Turnstile.Services.Cosmos;
using Turnstile.Services.EventGrid;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<IApiAuthorizationService, ApiAuthorizationService>();

        services.AddScoped<IPublisherConfigurationStore>(_ =>
            new BlobStoragePublisherConfigurationStore(
                BlobStorageConfiguration.FromPublisherConfigurationStorageEnvironmentVariables()));

        services.AddScoped<ISubscriptionEventPublisher>(_ =>
            new EventGridSubscriptionEventPublisher(
                EventGridConfiguration.FromEnvironmentVariables()));

        services.AddScoped<ITurnstileRepository>(_ =>
            new CosmosTurnstileRepository(
                CosmosConfiguration.FromEnvironmentVariables()));

        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

host.Run();
