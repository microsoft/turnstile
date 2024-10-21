using Microsoft.AspNetCore.Http;

namespace Turnstile.Api.Interfaces
{
    public interface IApiAuthorizationService
    {
        Task<bool> IsAuthorized(HttpRequest request);
    }
}
