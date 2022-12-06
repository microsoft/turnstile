using AzureFunctions.Extensions.Swashbuckle;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Turnstile.Api;
using Turnstile.Core.Interfaces;
using Turnstile.Services.Cosmos;

[assembly: WebJobsStartup(typeof(Startup))]
namespace Turnstile.Api
{
    internal class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.Services.AddScoped<ITurnstileRepository>(sp =>
                new CosmosTurnstileRepository(CosmosConfiguration.FromEnvironmentVariables()));

            builder.AddSwashBuckle(Assembly.GetExecutingAssembly());
        }
    }
}
