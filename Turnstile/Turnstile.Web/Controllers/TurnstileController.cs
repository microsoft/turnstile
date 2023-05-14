// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Turnstile.Core.Constants;
using Turnstile.Core.Extensions;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models.Configuration;
using Turnstile.Web.Extensions;
using Turnstile.Web.Models;

namespace Turnstile.Web.Controllers
{
    public class TurnstileController : Controller
    {
        public static class RouteNames
        {
            public const string DefaultTurnstile = "default_turnstile";
            public const string SpecificTurnstile = "specific_turnstile";
            public const string OnNoSubscriptions = "on_no_subscriptions";
            public const string OnAccessDenied = "on_access_denied";
            public const string OnSubscriptionNotFound = "on_subscription_not_found";
            public const string OnSubscriptionNotReady = "on_subscription_not_ready";
            public const string OnNoSeatsAvailable = "on_no_seats_available";
            public const string OnSubscriptionCanceled = "on_subscription_canceled";
            public const string OnSubscriptionSuspended = "on_subscription_suspended";
        }

        public static class ViewNames
        {
            public const string PickSubscription = "PickSubscription";
        }

        private readonly ILogger logger;
        private readonly IPublisherConfigurationClient pubConfigClient;
        private readonly ISeatsClient seatsClient;
        private readonly ISubscriptionsClient subsClient;

        private PublisherConfiguration? internalPublisherConfig;

        private async Task<PublisherConfiguration?> GetPublisherConfiguration() =>
            internalPublisherConfig ??= await pubConfigClient.GetConfiguration();

        public TurnstileController(
            ILogger<TurnstileController> logger,
            IPublisherConfigurationClient pubConfigClient,
            ISeatsClient seatsClient,
            ISubscriptionsClient subsClient)
        {
            this.logger = logger;
            this.pubConfigClient = pubConfigClient;
            this.seatsClient = seatsClient;
            this.subsClient = subsClient;
        }

        [HttpGet]
        [Route("", Name = RouteNames.DefaultTurnstile)]
        public async Task<IActionResult> DefaultTurnstile(string? returnTo = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(returnTo) &&
                    !Uri.TryCreate(returnTo, UriKind.Absolute, out _))
                {
                    // I don't know what you're trying to do here but, nice try, bad actor.

                    return RedirectToRoute(RouteNames.DefaultTurnstile);
                }

                var publisherConfig = await GetPublisherConfiguration();

                if (publisherConfig!.CheckTurnstileSetupIsComplete(User, logger) is var setupAction &&
                    setupAction != null)
                {
                    // I like this pattern. Common validation is done in an extension method
                    // instead of in an unecessary base class. If the validation method returns an
                    // action, that's the controller's cue to run it.

                    return setupAction;
                }
                else
                {
                    if (CheckForMsa(publisherConfig!) is var redirectAction && redirectAction != null)
                    {
                        // We can't help them (yet). Bounce the user to the SaaS app...

                        return redirectAction;
                    }

                    var user = User.ToCoreModel();

                    var availableSubs = (await subsClient.GetSubscriptions(user.TenantId))
                        .Where(s => s.IsActive() && s.IsSetupComplete == true && User.CanUseSubscription(s))
                        .ToList();

                    if (availableSubs.None())
                    {
                        logger.LogWarning($"User [{user.TenantId}/{user.UserId}] has no available subscriptions.");

                        return publisherConfig!.OnNoSubscriptionsFound();
                    }
                    else if (availableSubs.OnlyOne() && !User.CanAdministerSubscription(availableSubs.Single()))
                    { 
                        return RedirectToRoute(
                            RouteNames.SpecificTurnstile,
                            returnTo == null ?
                                new { subscriptionId = availableSubs[0].SubscriptionId } :
                                new { subscriptionId = availableSubs[0].SubscriptionId, returnTo });
                    }
                    else
                    {
                        ViewData.ApplyModel(new LayoutViewModel(publisherConfig!, User));

                        return View(ViewNames.PickSubscription, new PickSubscriptionViewModel(availableSubs, User, returnTo));
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception at [{RouteNames.DefaultTurnstile}]: [{ex.Message}]");

                throw;
            }
        }

        [HttpGet]
        [Route("turnstile/{subscriptionId}", Name = RouteNames.SpecificTurnstile)]
        public async Task<IActionResult> SpecificTurnstile(string subscriptionId, string? returnTo = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(returnTo) &&
                    !Uri.TryCreate(returnTo, UriKind.Absolute, out _))
                {
                    // I don't know what you're trying to do here but, nice try, bad actor.

                    return RedirectToRoute(RouteNames.SpecificTurnstile, new { subscriptionId });
                }

                var publisherConfig = await GetPublisherConfiguration();

                if (publisherConfig!.CheckTurnstileSetupIsComplete(User, logger) is var setupAction &&
                    setupAction != null)
                {
                    return setupAction;
                }

                if (CheckForMsa(publisherConfig!) is var redirectAction && redirectAction != null)
                {
                    // We can't help them (yet). Bounce the user to the SaaS app...

                    return redirectAction;
                }

                var seatRequest = User.CreateSeatRequest();
                var seatResult = await seatsClient.EnterTurnstile(seatRequest, subscriptionId);

                switch (seatResult!.ResultCode)
                {
                    case SeatResultCodes.SubscriptionSuspended:
                        return publisherConfig!.OnSubscriptionSuspended(subscriptionId);
                    case SeatResultCodes.SubscriptionNotReady:
                        return publisherConfig!.OnSubscriptionNotReady(subscriptionId);
                    case SeatResultCodes.SubscriptionNotFound:
                        return publisherConfig!.OnSubscriptionNotFound(subscriptionId);
                    case SeatResultCodes.SeatProvided:
                        return string.IsNullOrEmpty(returnTo)
                            ? publisherConfig!.OnAccessGranted(subscriptionId)
                            : Redirect(returnTo);
                    case SeatResultCodes.SubscriptionCanceled:
                        return publisherConfig!.OnSubscriptionCanceled(subscriptionId);
                    case SeatResultCodes.AccessDenied:
                        return publisherConfig!.OnAccessDenied(subscriptionId);
                    case SeatResultCodes.NoSeatsAvailable:
                        return publisherConfig!.OnNoSeatsAvailable(subscriptionId);
                    default:
                        throw new InvalidOperationException($"Unable to handle turnstile result code [{seatResult!.ResultCode}].");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception at [{RouteNames.SpecificTurnstile}]: [{ex.Message}]");

                throw;
            }
        }

        [HttpGet]
        [Route("turnstile/on/no-subscriptions", Name = RouteNames.OnNoSubscriptions)]
        public async Task<IActionResult> OnNoSubscriptions()
        {
            try
            {
                var publisherConfig = await GetPublisherConfiguration();

                ViewData.ApplyModel(new LayoutViewModel(publisherConfig!, User));

                return View();
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ [{nameof(OnNoSubscriptions)}]: [{ex.Message}]");

                throw;
            }
        }

        [HttpGet]
        [Route("turnstile/on/subscription-canceled/{subscriptionId}", Name = RouteNames.OnSubscriptionCanceled)]
        public async Task<IActionResult> OnSubscriptionCanceled(string subscriptionId)
        {
            try
            {
                var publisherConfig = await GetPublisherConfiguration();
                var subscription = await subsClient.GetSubscription(subscriptionId);

                if (subscription == null)
                {
                    return RedirectToRoute(RouteNames.OnSubscriptionNotFound, new { subscriptionId });
                }
                else
                {
                    ViewData.ApplyModel(new LayoutViewModel(publisherConfig!, User));
                    ViewData.ApplyModel(new SubscriptionContextViewModel(subscription!, User));

                    return View();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ [{nameof(OnSubscriptionCanceled)}]: [{ex.Message}]");

                throw;
            }
        }

        [HttpGet]
        [Route("turnstile/on/subscription-not-ready/{subscriptionId}", Name = RouteNames.OnSubscriptionNotReady)]
        public async Task<IActionResult> OnSubscriptionNotReady(string subscriptionId)
        {
            try
            {
                var publisherConfig = await GetPublisherConfiguration();
                var subscription = await subsClient.GetSubscription(subscriptionId);

                if (subscription == null)
                {
                    return RedirectToRoute(RouteNames.OnSubscriptionNotFound, new { subscriptionId });
                }
                else
                {
                    ViewData.ApplyModel(new LayoutViewModel(publisherConfig!, User));
                    ViewData.ApplyModel(new SubscriptionContextViewModel(subscription!, User));

                    return View();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ [{nameof(OnSubscriptionNotReady)}]: [{ex.Message}]");

                throw;
            }
        }

        [HttpGet]
        [Route("turnstile/on/subscription-suspended/{subscriptionId}", Name = RouteNames.OnSubscriptionSuspended)]
        public async Task<IActionResult> OnSubscriptionSuspended(string subscriptionId)
        {
            try
            {
                var publisherConfig = await GetPublisherConfiguration();
                var subscription = await subsClient.GetSubscription(subscriptionId);

                if (subscription == null)
                {
                    return RedirectToRoute(RouteNames.OnSubscriptionNotFound, new { subscriptionId });
                }
                else
                {
                    ViewData.ApplyModel(new LayoutViewModel(publisherConfig!, User));
                    ViewData.ApplyModel(new SubscriptionContextViewModel(subscription!, User));

                    return View();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ [{nameof(OnSubscriptionSuspended)}]: [{ex.Message}]");

                throw;
            }
        }

        [HttpGet]
        [Route("turnstile/on/subscription-not-found/{subscriptionId}", Name = RouteNames.OnSubscriptionNotFound)]
        public async Task<IActionResult> OnSubscriptionNotFound(string subscriptionId)
        {
            try
            {
                var publisherConfig = await GetPublisherConfiguration();

                ViewData.ApplyModel(new LayoutViewModel(publisherConfig!, User));

                return View();
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ [{nameof(OnSubscriptionNotFound)}]: [{ex.Message}]");

                throw;
            }
        }

        [Route("turnstile/on/no-seats/{subscriptionId}", Name = RouteNames.OnNoSeatsAvailable)]
        public async Task<IActionResult> OnNoSeatsAvailable(string subscriptionId)
        {
            try
            {
                var publisherConfig = await GetPublisherConfiguration();
                var subscription = await subsClient.GetSubscription(subscriptionId);

                if (subscription == null)
                {
                    return RedirectToRoute(RouteNames.OnSubscriptionNotFound, new { subscriptionId });
                }
                else
                {
                    ViewData.ApplyModel(new LayoutViewModel(publisherConfig!, User));
                    ViewData.ApplyModel(new SubscriptionContextViewModel(subscription!, User));

                    return View();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ [{nameof(OnNoSeatsAvailable)}]: [{ex.Message}]");

                throw;
            }
        }

        private IActionResult? CheckForMsa(PublisherConfiguration publisherConfig)
        {
            if (User.IsMsa())
            {
                logger.LogWarning($"MSA [{User.GetObjectId()}] trying to use Turnstile. MSAs are not currently supported. Redirecting user directly to SaaS app...");

                // So... Turnstile doesn't currently support MSAs but Mona does. Although they don't have to be, Mona and Turnstile are designed to be deployed together so
                // we need a way to gracefully handle MSAs here inside Turnstile without actually providing them with seats. In the future, Turnstile will be able 
                // to support different kinds of accounts (including B2C, et al.) but, for right now, we just do organizational accounts. If we happen to get an MSA,
                // we just redirect them to the front door of the customer's SaaS app to let them handle them.

                return Redirect(publisherConfig!.TurnstileConfiguration!.OnAccessGrantedUrl!);
            }

            return null;
        }
    }
}
