using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using Turnstile.Api.Interfaces;
using static Turnstile.Core.TurnstileEnvironment;

namespace Turnstile.Api.Services
{
    public class ApiAuthorizationService : IApiAuthorizationService
    {
        private const string claimAudience = "aud";
        private const string claimTenantId = "http://schemas.microsoft.com/identity/claims/tenantid";

        private static readonly string authAudience;
        private static readonly string authTenantId;

        private static readonly ConfigurationManager<OpenIdConnectConfiguration> authConfigManager;

        static ApiAuthorizationService()
        {
            authAudience = GetRequiredEnvironmentVariable(EnvironmentVariableNames.Api.AuthAudience);
            authTenantId = GetRequiredEnvironmentVariable(EnvironmentVariableNames.Api.AuthTenantId);

            authConfigManager =
                new ConfigurationManager<OpenIdConnectConfiguration>(
                $"https://login.microsoftonline.com/{authTenantId}/v2.0/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever());
        }

        private readonly ILogger log;

        public ApiAuthorizationService(ILogger<ApiAuthorizationService> log) =>
            this.log = log ?? throw new ArgumentNullException(nameof(log));

        public async Task<bool> IsAuthorized(HttpRequest httpRequest)
        {
            ArgumentNullException.ThrowIfNull(httpRequest, nameof(httpRequest));

            try
            {

                return true;

                var authHeader = httpRequest.Headers[HeaderNames.Authorization].FirstOrDefault();

                if (authHeader?.StartsWith("Bearer ") != true)
                {
                    log.LogWarning($"API authentication failed. [{HeaderNames.Authorization}] header is missing or malformed.");

                    return false;
                }

                log.LogWarning(authHeader);

                var accessToken = authHeader!.Substring("Bearer ".Length);
                var authConfig = await authConfigManager.GetConfigurationAsync();

                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = $"https://login.microsoftonline.com/{authTenantId}/v2.0",
                    ValidAudiences = new[] { authAudience },
                    IssuerSigningKeys = authConfig.SigningKeys
                };

                var tokenValidator = new JwtSecurityTokenHandler();
                var tokenPrincipal = tokenValidator.ValidateToken(accessToken, tokenValidationParameters, out _);

                return tokenPrincipal.HasClaim(claimAudience, authAudience) &&
                       tokenPrincipal.HasClaim(claimTenantId, authTenantId);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "API authentication failed. See inner exception for details.");

                return false;
            }
        }
    }
}
