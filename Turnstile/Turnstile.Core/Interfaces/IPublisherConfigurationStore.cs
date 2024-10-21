using Turnstile.Core.Models.Configuration;

namespace Turnstile.Core.Interfaces
{
    public interface IPublisherConfigurationStore
    {
        Task<PublisherConfiguration?> GetConfiguration();
        Task<PublisherConfiguration> PutConfiguration(PublisherConfiguration publisherConfig);
    }
}
