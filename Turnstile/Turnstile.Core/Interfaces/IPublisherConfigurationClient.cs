// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Turnstile.Core.Models.Configuration;

namespace Turnstile.Core.Interfaces
{
    public interface IPublisherConfigurationClient
    {
        Task<PublisherConfiguration?> GetConfiguration();
        Task UpdateConfiguration(PublisherConfiguration configuration);
    }
}
