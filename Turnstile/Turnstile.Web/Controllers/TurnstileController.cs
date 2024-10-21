// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using System.Net;
using Turnstile.Core.Constants;
using Turnstile.Core.Extensions;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Configuration;
using Turnstile.Web.Common.Extensions;
using Turnstile.Web.Common.Models;
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

                if (publisherConfig?.IsSetupComplete == true)
                {
                    if (CheckForMsa(publisherConfig!) is var redirectAction && redirectAction != null)
                    {
                        // We can't help them (yet). Bounce the user to the SaaS app...

                        return redirectAction;
                    }

                    var user = User.ToCoreModel(publisherConfig.ClaimsConfiguration!);
                    var availableSubs = (await subsClient.GetSubscriptions(user.TenantId)).ToList();
                    var pickSubsModel = new PickSubscriptionViewModel(availableSubs, User, publisherConfig.ClaimsConfiguration!, returnTo);

                    if (!pickSubsModel.Any())
                    {
                        // There's literally nothing here they have access to.

                        logger.LogWarning($"User [{user.TenantId}/{user.UserId}] has no available subscriptions.");

                        return publisherConfig!.OnNoSubscriptionsFound();
                    }
                    else if (pickSubsModel.UsableSubscriptions.Count == 1 && pickSubsModel.ManageableSubscriptions.None())
                    {
                        // In production, this is probably the most common case. Someone wants to obtain a seat in the
                        // subscription and use it. It's the only thing they know about and they only thing they can do.
                        // Our goal is to make this path as insivible to the user as possible.

                        // If the user can't _manage_ any subscriptions but they can only _use_ one subscription
                        // (in reality, probably the most likely use case for users hitting Turnstile), we keep things
                        // real simple and redirect them immediately to use the SaaS app.

                        var usableSub = pickSubsModel.UsableSubscriptions.First()!;

                        return RedirectToRoute(
                            RouteNames.SpecificTurnstile,
                            returnTo == null ?
                                new { subscriptionId = usableSub.SubscriptionId } :
                                new { subscriptionId = usableSub.SubscriptionId, returnTo });
                    }
                    else if (pickSubsModel.UsableSubscriptions.None() && pickSubsModel.ManageableSubscriptions.Count == 1)
                    {
                        // After the customer has purchased a subscription, the next thing they'll need to do is set it up.
                        // This path covers that case and will probably be followed only once per subscription because, once
                        // the subscription is set up, chances are the administrator will also be able to use it. In that case,
                        // we fall to the case below and let them pick whether they want to administer or use the subscription.

                        // If the user can't _use_ any subscriptions but can _manage_ one and only one, there's a pretty good
                        // chance they just purchased it and need to set it up. Why make this any more complicated than it needs
                        // to be? Rediret them to the subscription setup page...

                        var manageableSub = pickSubsModel.ManageableSubscriptions.First()!;

                        return RedirectToRoute(
                            SubscriptionsController.RouteNames.GetSubscription,
                            new { subscriptionId = manageableSub.SubscriptionId });
                    }
                    else
                    {
                        // Let them pick...

                        this.ApplyModel(new LayoutViewModel(publisherConfig!));

                        return View(ViewNames.PickSubscription, pickSubsModel);
                    }
                }
                else
                {
                    return this.ServiceUnavailable();
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

                if (publisherConfig?.IsSetupComplete == true)
                {
                    if (CheckForMsa(publisherConfig!) is var redirectAction && redirectAction != null)
                    {
                        // We can't help them (yet). Bounce the user to the SaaS app...

                        return redirectAction;
                    }

                    var seatRequest = User.CreateSeatRequest(publisherConfig.ClaimsConfiguration!);
                    var seatResult = await seatsClient.EnterTurnstile(seatRequest, subscriptionId);

                    return seatResult!.ResultCode switch
                    {
                        SeatResultCodes.SubscriptionSuspended => publisherConfig!.OnSubscriptionSuspended(subscriptionId),
                        SeatResultCodes.SubscriptionNotReady => publisherConfig!.OnSubscriptionNotReady(subscriptionId),
                        SeatResultCodes.SubscriptionNotFound => publisherConfig!.OnSubscriptionNotFound(subscriptionId),
                        SeatResultCodes.SeatProvided => await OnSeatProvided(seatResult!, subscriptionId, returnTo),
                        SeatResultCodes.SubscriptionCanceled => publisherConfig!.OnSubscriptionCanceled(subscriptionId),
                        SeatResultCodes.AccessDenied => publisherConfig!.OnAccessDenied(subscriptionId),
                        SeatResultCodes.NoSeatsAvailable => publisherConfig!.OnNoSeatsAvailable(subscriptionId),
                        _ => throw new InvalidOperationException($"Unable to handle turnstile seat result code [{seatResult!.ResultCode}].")
                    };
                }
                else
                {
                    return this.ServiceUnavailable();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception at [{RouteNames.SpecificTurnstile}]: [{ex.Message}]");

                throw;
            }
        }

        private async Task<IActionResult> OnSeatProvided(SeatResult seatResult, string subscriptionId, string? returnTo = null)
        {
            var publisherConfig = (await GetPublisherConfiguration())!;

            var redirectUrl = returnTo
                ?? publisherConfig.TurnstileConfiguration?.OnAccessGrantedUrl
                ?? publisherConfig.AppUrl!;

            redirectUrl = redirectUrl
                .MergeSubscriptionId(subscriptionId)
                .MergeSeatId(seatResult.Seat!.SeatId!);

            return Redirect(redirectUrl);
        }

        [HttpGet]
        [Route("turnstile/on/no-subscriptions", Name = RouteNames.OnNoSubscriptions)]
        public async Task<IActionResult> OnNoSubscriptions()
        {
            try
            {
                var publisherConfig = await GetPublisherConfiguration();

                if (publisherConfig?.IsSetupComplete == true)
                {
                    this.ApplyModel(new LayoutViewModel(publisherConfig));

                    return View();
                }
                else
                {
                    return this.ServiceUnavailable();
                }
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

                if (publisherConfig?.IsSetupComplete == true)
                {
                    var subscription = await subsClient.GetSubscription(subscriptionId);

                    if (subscription == null)
                    {
                        return RedirectToRoute(RouteNames.OnSubscriptionNotFound, new { subscriptionId });
                    }
                    else
                    {
                        this.ApplyModel(new LayoutViewModel(publisherConfig!));
                        this.ApplyModel(new SubscriptionContextViewModel(subscription!, User!, publisherConfig.ClaimsConfiguration!));

                        return View();
                    }
                }
                else
                {
                    return this.ServiceUnavailable();
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

                if (publisherConfig?.IsSetupComplete == true)
                {
                    var subscription = await subsClient.GetSubscription(subscriptionId);

                    if (subscription == null)
                    {
                        return RedirectToRoute(RouteNames.OnSubscriptionNotFound, new { subscriptionId });
                    }
                    else
                    {
                        this.ApplyModel(new LayoutViewModel(publisherConfig!));
                        this.ApplyModel(new SubscriptionContextViewModel(subscription!, User!, publisherConfig.ClaimsConfiguration!));

                        return View();
                    }
                }
                else
                {
                    return this.ServiceUnavailable();
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

                if (publisherConfig?.IsSetupComplete == true)
                {
                    var subscription = await subsClient.GetSubscription(subscriptionId);

                    if (subscription == null)
                    {
                        return RedirectToRoute(RouteNames.OnSubscriptionNotFound, new { subscriptionId });
                    }
                    else
                    {
                        this.ApplyModel(new LayoutViewModel(publisherConfig!));
                        this.ApplyModel(new SubscriptionContextViewModel(subscription!, User!, publisherConfig.ClaimsConfiguration!));

                        return View();
                    }
                }
                else
                {
                    return this.ServiceUnavailable();
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

                if (publisherConfig?.IsSetupComplete == true)
                {
                    this.ApplyModel(new LayoutViewModel(publisherConfig));

                    return View();
                }
                else
                {
                    return this.ServiceUnavailable();
                }
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

                if (publisherConfig?.IsSetupComplete == true)
                {
                    var subscription = await subsClient.GetSubscription(subscriptionId);

                    if (subscription == null)
                    {
                        return RedirectToRoute(RouteNames.OnSubscriptionNotFound, new { subscriptionId });
                    }
                    else
                    {
                        this.ApplyModel(new LayoutViewModel(publisherConfig!));
                        this.ApplyModel(new SubscriptionContextViewModel(subscription!, User!, publisherConfig.ClaimsConfiguration!));

                        return View();
                    }
                }
                else
                {
                    return this.ServiceUnavailable();
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

                return Redirect(publisherConfig!.AppUrl ?? publisherConfig!.TurnstileConfiguration!.OnAccessGrantedUrl!);
            }

            return null;
        }
    }
}
