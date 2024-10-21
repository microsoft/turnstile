// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using Turnstile.Core.Models.Configuration;
using Turnstile.Web.Controllers;

namespace Turnstile.Web.Extensions
{
    public static class PublisherConfigurationExtensions
    { 
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
                    new { subscriptionId });
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
                    new { subscriptionId });
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
                    new { subscriptionId });
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
                    new { subscriptionId });
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
                    new { subscriptionId });
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
                    new { subscriptionId });
            }
            else
            {
                return new RedirectResult(MergeSubscriptionId(
                    publisherConfig.TurnstileConfiguration!.OnNoSeatAvailableUrl,
                    subscriptionId));
            }
        }

        public static string MergeSeatId(this string intoString, string seatId) =>
            intoString.Replace("{seat_id}", seatId);

        public static string MergeSubscriptionId(this string intoString, string subscriptionId) =>
            intoString.Replace("{subscription_id}", subscriptionId);
    }
}
