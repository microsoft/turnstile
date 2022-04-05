using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using Turnstile.Core.Models.Configuration;
using Turnstile.Web.Models;

namespace Turnstile.Web.Extensions
{
    public static class ControllerExtensions
    {
        public static IActionResult ServiceUnavailable(this Controller controller) => 
            new StatusCodeResult((int)HttpStatusCode.ServiceUnavailable);

        public static void ApplyLayout(this Controller controller, PublisherConfiguration publisherConfig, ClaimsPrincipal forPrincipal)
        {
            ArgumentNullException.ThrowIfNull(controller, nameof(controller));
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));
            ArgumentNullException.ThrowIfNull(forPrincipal, nameof(forPrincipal));

            controller.ViewData[nameof(LayoutViewModel)] = new LayoutViewModel(publisherConfig, forPrincipal);
        }
    }
}
