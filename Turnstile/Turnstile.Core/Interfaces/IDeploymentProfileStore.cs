using Turnstile.Core.Models.Profiles;

namespace Turnstile.Core.Interfaces
{
    public interface IDeploymentProfileStore
    {
        Task<DeploymentProfile?> GetDeploymentProfile();
    }
}
