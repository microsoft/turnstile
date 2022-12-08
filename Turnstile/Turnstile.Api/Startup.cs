// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Turnstile.Api;
using Turnstile.Core.Interfaces;
using Turnstile.Services.Cosmos;

[assembly: FunctionsStartup(typeof(Startup))]
namespace Turnstile.Api
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddScoped<ITurnstileRepository>(sp =>
                new CosmosTurnstileRepository(CosmosConfiguration.FromEnvironmentVariables()));
        }
    }
}
