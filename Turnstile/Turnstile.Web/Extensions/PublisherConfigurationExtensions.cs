using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using Turnstile.Core.Models.Configuration;
using Turnstile.Web.Controllers;

namespace Turnstile.Web.Extensions
{
    public static class PublisherConfigurationExtensions
    {
        public static IActionResult? CheckTurnstileSetupIsComplete(
            this PublisherConfiguration publisherConfig,
            ClaimsPrincipal principal,
            ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));
            ArgumentNullException.ThrowIfNull(principal, nameof(principal));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            if (publisherConfig.IsSetupComplete)
            {
                return null;
            }
            else
            {
                if (principal.CanAdministerTurnstile())
                {
                    // If they can set it up, they should...

                    logger.LogWarning(
                        "Unable to service request. Turnstile not yet set up. " +
                        $"Redirecting turnstile administrator to route [{PublisherConfigurationController.RouteNames.GetPublisherConfiguration}].");

                    return new RedirectToRouteResult(PublisherConfigurationController.RouteNames.GetPublisherConfiguration, null);
                }
                else
                {
                    logger.LogWarning("Unable to service request. Turnstile not yet set up. Responding 503 Service Unavailable.");

                    return new StatusCodeResult((int)(HttpStatusCode.ServiceUnavailable));
                }
            }
        }
    }
}
