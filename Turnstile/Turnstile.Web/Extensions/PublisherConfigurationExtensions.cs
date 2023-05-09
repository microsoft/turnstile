// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using Turnstile.Core.Models.Configuration;
using Turnstile.Web.Controllers;
using Turnstile.Web.Models.PublisherConfig;

namespace Turnstile.Web.Extensions
{
    public static class PublisherConfigurationExtensions
    {
        public static PublisherConfiguration Apply(this PublisherConfiguration publisherConfig, BasicConfigurationViewModel basicConfig)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));
            ArgumentNullException.ThrowIfNull(basicConfig, nameof(basicConfig));

            publisherConfig.ContactPageUrl = basicConfig.ContactPageUrl;
            publisherConfig.ContactSalesEmail = basicConfig.ContactSalesEmail;
            publisherConfig.ContactSalesUrl = basicConfig.ContactSalesUrl;
            publisherConfig.ContactSupportEmail = basicConfig.ContactSupportEmail;
            publisherConfig.ContactSupportUrl = basicConfig.ContactSupportUrl;
            publisherConfig.HomePageUrl = basicConfig.HomePageUrl;
            publisherConfig.PrivacyNoticePageUrl = basicConfig.PrivacyNoticePageUrl;
            publisherConfig.PublisherName = basicConfig.PublisherName;
            publisherConfig.TurnstileName = basicConfig.TurnstileName;

            return publisherConfig;
        }

        public static PublisherConfiguration Apply(this PublisherConfiguration publisherConfig, MonaConfigurationViewModel monaConfig)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));
            ArgumentNullException.ThrowIfNull(monaConfig, nameof(monaConfig));

            publisherConfig.MonaBaseStorageUrl = monaConfig.MonaIntegrationBaseStorageUrl;
            publisherConfig.DefaultMonaSubscriptionState = monaConfig.DefaultMonaSubscriptionState;
            publisherConfig.MonaSubscriptionIsBeingConfigured = monaConfig.MonaSubscriptionIsBeingConfigured;

            return publisherConfig;
        }

        public static PublisherConfiguration Apply(this PublisherConfiguration publisherConfig, RedirectConfigurationViewModel redirectConfig)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));
            ArgumentNullException.ThrowIfNull(redirectConfig, nameof(redirectConfig));

            publisherConfig.TurnstileConfiguration = new TurnstileConfiguration
            {
                OnNoSeatAvailableUrl = redirectConfig.OnNoSeatsAvailableUrl,
                OnAccessDeniedUrl = redirectConfig.OnAccessDeniedUrl,
                OnAccessGrantedUrl = redirectConfig.OnAccessGrantedUrl,
                OnNoSubscriptionsFoundUrl = redirectConfig.OnNoSubscriptionsFoundUrl,
                OnSubscriptionCanceledUrl = redirectConfig.OnSubscriptionCanceledUrl,
                OnSubscriptionNotFoundUrl = redirectConfig.OnSubscriptionNotFoundUrl,
                OnSubscriptionNotReadyUrl = redirectConfig.OnSubscriptionPurchasedUrl,
                OnSubscriptionSuspendedUrl = redirectConfig.OnSubscriptionSuspendedUrl
            };

            return publisherConfig;
        }

        public static PublisherConfiguration Apply(this PublisherConfiguration publisherConfig, SeatingConfigurationViewModel seatingConfig)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));
            ArgumentNullException.ThrowIfNull(seatingConfig, nameof(seatingConfig));

            publisherConfig.SeatingConfiguration ??= new SeatingConfiguration();
            publisherConfig.SeatingConfiguration.LimitedOverflowSeatingEnabled = seatingConfig.LimitedOverflowSeatingEnabled;
            publisherConfig.SeatingConfiguration.SeatingStrategyName = seatingConfig.SeatingStrategyName;

            return publisherConfig;
        }

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
                        $"Redirecting turnstile administrator to route [{PublisherConfigurationController.RouteNames.ConfigureBasics}].");

                    return new RedirectToRouteResult(PublisherConfigurationController.RouteNames.ConfigureBasics, null);
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

        public static IActionResult OnSubscriptionNotReady(this PublisherConfiguration publisherConfig, string subscriptionId)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));
            ArgumentNullException.ThrowIfNull(subscriptionId, nameof(subscriptionId));

            if (string.IsNullOrEmpty(publisherConfig.TurnstileConfiguration?.OnSubscriptionNotReadyUrl))
            {
                return new RedirectToRouteResult(
                    TurnstileController.RouteNames.OnSubscriptionNotReady,
                    new { subscriptionId = subscriptionId });
            }
            else
            {
                return new RedirectResult(MergeSubscriptionId(
                    publisherConfig.TurnstileConfiguration!.OnSubscriptionNotReadyUrl,
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
                return new ForbidResult(); 
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
