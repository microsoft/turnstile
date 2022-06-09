// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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

        public static IActionResult OnNoSubscriptionsFound(this PublisherConfiguration publisherConfig)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));

            if (string.IsNullOrEmpty(publisherConfig.TurnstileConfiguration?.OnNoSubscriptionsFoundUrl))
            {
                return new RedirectToRouteResult(TurnstileController.RouteNames.OnNoSubscriptions, null);
            }
            else
            {
                return new RedirectResult(publisherConfig.TurnstileConfiguration!.OnNoSubscriptionsFoundUrl!);
            }
        }

        public static IActionResult OnSubscriptionCanceled(this PublisherConfiguration publisherConfig, string subscriptionId)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));
            ArgumentNullException.ThrowIfNull(subscriptionId, nameof(subscriptionId));

            if (string.IsNullOrEmpty(publisherConfig.TurnstileConfiguration?.OnSubscriptionCanceledUrl))
            {
                return new RedirectToRouteResult(
                    TurnstileController.RouteNames.OnSubscriptionCanceled,
                    new { subscriptionId = subscriptionId });
            }
            else
            {
                return new RedirectResult(MergeSubscriptionId(
                    publisherConfig.TurnstileConfiguration!.OnSubscriptionCanceledUrl!,
                    subscriptionId));
            }
        }

        public static IActionResult OnSubscriptionSuspended(this PublisherConfiguration publisherConfig, string subscriptionId)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));
            ArgumentNullException.ThrowIfNull(subscriptionId, nameof(subscriptionId));

            if (string.IsNullOrEmpty(publisherConfig.TurnstileConfiguration?.OnSubscriptionSuspendedUrl))
            {
                return new RedirectToRouteResult(
                    TurnstileController.RouteNames.OnSubscriptionSuspended,
                    new { subscriptionId = subscriptionId });
            }
            else
            {
                return new RedirectResult(MergeSubscriptionId(
                    publisherConfig.TurnstileConfiguration!.OnSubscriptionSuspendedUrl,
                    subscriptionId));
            }
        }

        public static IActionResult OnSubscriptionNotFound(this PublisherConfiguration publisherConfig, string subscriptionId)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));
            ArgumentNullException.ThrowIfNull(subscriptionId, nameof(subscriptionId));

            if (string.IsNullOrEmpty(publisherConfig.TurnstileConfiguration?.OnSubscriptionNotFoundUrl))
            {
                return new RedirectToRouteResult(
                    TurnstileController.RouteNames.OnSubscriptionNotFound,
                    new { subscriptionId = subscriptionId });
            }
            else
            {
                return new RedirectResult(MergeSubscriptionId(
                    publisherConfig.TurnstileConfiguration!.OnSubscriptionNotFoundUrl,
                    subscriptionId));
            }
        }

        public static IActionResult OnAccessDenied(this PublisherConfiguration publisherConfig, string subscriptionId)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));
            ArgumentNullException.ThrowIfNull(subscriptionId, nameof(subscriptionId));

            if (string.IsNullOrEmpty(publisherConfig.TurnstileConfiguration?.OnAccessDeniedUrl))
            {
                return new RedirectToRouteResult(
                    TurnstileController.RouteNames.OnAccessDenied,
                    new { subscriptionId = subscriptionId });
            }
            else
            {
                return new RedirectResult(MergeSubscriptionId(
                    publisherConfig.TurnstileConfiguration!.OnAccessDeniedUrl,
                    subscriptionId));
            }
        }

        public static IActionResult OnNoSeatsAvailable(this PublisherConfiguration publisherConfig, string subscriptionId)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));
            ArgumentNullException.ThrowIfNull(subscriptionId, nameof(subscriptionId));

            if (string.IsNullOrEmpty(publisherConfig.TurnstileConfiguration?.OnNoSeatAvailableUrl))
            {
                return new RedirectToRouteResult(
                    TurnstileController.RouteNames.OnNoSeatsAvailable,
                    new { subscriptionId = subscriptionId });
            }
            else
            {
                return new RedirectResult(MergeSubscriptionId(
                    publisherConfig.TurnstileConfiguration!.OnNoSeatAvailableUrl,
                    subscriptionId));
            }
        }

        public static IActionResult OnAccessGranted(this PublisherConfiguration publisherConfig, string subscriptionId)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));
            ArgumentNullException.ThrowIfNull(subscriptionId, nameof(subscriptionId));

            return new RedirectResult(MergeSubscriptionId(
                publisherConfig.TurnstileConfiguration!.OnAccessGrantedUrl!,
                subscriptionId));
        }

        private static string MergeSubscriptionId(string intoString, string subscriptionId) =>
            intoString.Replace("{subscription_id}", subscriptionId);
    }
}
