// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using Turnstile.Core.Constants;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;
using Turnstile.Web.Extensions;
using Turnstile.Web.Models;

namespace Turnstile.Web.Controllers
{
    public class SubscriptionsController : Controller
    {
        public static class RouteNames
        {
            public const string GetSubscription = "subscription";
            public const string GetSubscriptions = "subscriptions";
            public const string GetReserveSeat = "reserve_seat";
        }

        private readonly ILogger logger;
        private readonly IPublisherConfigurationClient publisherConfigClient;
        private readonly ISeatsClient seatsClient;
        private readonly ISubscriptionsClient subsClient;

        public SubscriptionsController(
            ILogger<SubscriptionsController> logger,
            IPublisherConfigurationClient publisherConfigClient,
            ISeatsClient seatsClient,
            ISubscriptionsClient subsClient)
        {
            this.logger = logger;
            this.publisherConfigClient = publisherConfigClient;
            this.seatsClient = seatsClient;
            this.subsClient = subsClient;
        }

        [HttpGet]
        [Route("subscriptions", Name = RouteNames.GetSubscriptions)]
        public async Task<IActionResult> Subscriptions(string? sort = null)
        {
            try
            {
                var publisherConfig = await publisherConfigClient.GetConfiguration();

                if (publisherConfig!.CheckTurnstileSetupIsComplete(User, logger) is var setupAction &&
                    setupAction != null)
                {
                    return setupAction;
                }

                this.ApplyLayout(publisherConfig!, User!);

                var subUser = User.ToCoreModel();
                var subscriptions = new List<Subscription>();

                if (User.CanAdministerTurnstile())
                {
                    subscriptions.AddRange(await subsClient.GetSubscriptions());
                }
                else
                {
                    var tenantSubs = await subsClient.GetSubscriptions(subUser.TenantId!);
                    subscriptions.AddRange(tenantSubs.Where(s => User.CanAdministerSubscription(s)));
                }

                if (subscriptions.Any())

                {
                    return View(new SubscriptionsViewModel(subscriptions));
                }
                else
                {
                    return RedirectToRoute(TurnstileController.RouteNames.OnNoSubscriptions);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ GET [{nameof(Subscriptions)}]: [{ex.Message}]");

                throw;
            }
        }

        [HttpGet]
        [Route("subscriptions/{subscriptionId}", Name = RouteNames.GetSubscription)]
        public async Task<IActionResult> Subscription(string subscriptionId)
        {
            try
            {
                var publisherConfig = await publisherConfigClient.GetConfiguration();

                if (publisherConfig!.CheckTurnstileSetupIsComplete(User, logger) is var setupAction &&
                    setupAction != null)
                {
                    return setupAction;
                }

                var subscription = await subsClient.GetSubscription(subscriptionId);

                if (subscription == null)
                {
                    return publisherConfig!.OnSubscriptionNotFound(subscriptionId);
                }

                var isTurnstileAdmin = User.CanAdministerTurnstile();
                var isSubscriberAdmin = User.CanAdministerSubscription(subscription);

                if (isTurnstileAdmin || isSubscriberAdmin)
                {
                    this.ApplyLayout(publisherConfig!, User!);

                    var seats = await seatsClient.GetSeats(subscriptionId);

                    return View(new SubscriptionDetailViewModel(subscription, seats, isTurnstileAdmin, isSubscriberAdmin));
                }
                else
                {
                    return Forbid();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ GET [{nameof(Subscription)}]: [{ex.Message}]");

                throw;
            }
        }

        [HttpPost]
        [Route("subscriptions/{subscriptionId}")]
        public async Task<IActionResult> Subscription(string subscriptionId, [FromBody] SubscriptionDetailViewModel subscriptionDetail)
        {
            try
            {
                var publisherConfig = await publisherConfigClient.GetConfiguration();

                if (publisherConfig!.CheckTurnstileSetupIsComplete(User, logger) is var setupAction &&
                    setupAction != null)
                {
                    return setupAction;
                }

                var subscription = await subsClient.GetSubscription(subscriptionId);

                if (subscription == null)
                {
                    return publisherConfig!.OnSubscriptionNotFound(subscriptionId);
                }

                if (User.CanAdministerSubscription(subscription))
                {
                    if (ModelState.IsValid)
                    {
                        subscription.ApplyUpdate(subscriptionDetail);

                        await subsClient.UpdateSubscription(subscription);

                        subscriptionDetail.IsSubscriptionUpdated = true;
                        subscriptionDetail.HasValidationErrors = false;
                    }
                    else
                    {
                        subscriptionDetail.IsSubscriptionUpdated = false;
                        subscriptionDetail.HasValidationErrors = true;
                    }

                    return View(subscriptionDetail);
                }
                else
                {
                    return Forbid();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ POST [{nameof(Subscription)}]: [{ex.Message}]");

                throw;
            }
        }

        [HttpGet]
        [Route("subscriptions/{subscriptionId}/reserve-seat", Name = RouteNames.GetReserveSeat)]
        public async Task<IActionResult> ReserveSeat(string subscriptionId)
        {
            try
            {
                var publisherConfig = await publisherConfigClient.GetConfiguration();

                if (publisherConfig!.CheckTurnstileSetupIsComplete(User, logger) is var setupAction &&
                    setupAction != null)
                {
                    return setupAction;
                }

                var subscription = await subsClient.GetSubscription(subscriptionId);

                if (subscription == null)
                {
                    return publisherConfig!.OnSubscriptionNotFound(subscriptionId);
                }

                if (subscription.State != SubscriptionStates.Active) // Seats can be reserved only in active subscriptions.
                {
                    return RedirectToRoute(RouteNames.GetSubscription, new { subscriptionId = subscription.SubscriptionId });
                }

                if (User.CanAdministerSubscription(subscription))
                {
                    this.ApplyLayout(publisherConfig!, User!);

                    return View(new ReserveSeatViewModel(subscription));
                }
                else
                {
                    return Forbid();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ GET [{nameof(ReserveSeat)}]: [{ex.Message}]");

                throw;
            }
        }

        [HttpPost]
        [Route("subscriptions/{subscriptionId}/reserve-seat")]
        public async Task<IActionResult> ReserveSeat(string subscriptionId, [FromForm] ReserveSeatViewModel model)
        {
            try
            {
                var publisherConfig = await publisherConfigClient.GetConfiguration();

                if (publisherConfig!.CheckTurnstileSetupIsComplete(User, logger) is var setupAction &&
                    setupAction != null)
                {
                    return setupAction;
                }

                var subscription = await subsClient.GetSubscription(subscriptionId);

                if (subscription == null)
                {
                    return publisherConfig!.OnSubscriptionNotFound(subscriptionId);
                }

                if (User.CanAdministerSubscription(subscription))
                {
                    if (!string.IsNullOrEmpty(model.ForEmail))
                    {
                        var existingSeat = await seatsClient.GetSeatByEmail(subscriptionId, model.ForEmail!);

                        if (existingSeat != null)
                        {
                            ModelState.AddModelError(nameof(model.ForEmail), $"[{model.ForEmail!}] already has a seat in this subscription.");
                        }
                        else
                        {
                            var seat = await seatsClient.ReserveSeat(subscriptionId, new Reservation 
                            { 
                                Email = model.ForEmail!,
                                InvitationUrl = CreateInvitationLink(subscriptionId, model.ForEmail!)
                            });

                            if (seat == null)
                            {
                                ModelState.AddModelError(nameof(model.ForEmail), "No seats available to reserve in this subscription.");
                            }
                        }
                    }

                    if (ModelState.IsValid)
                    {
                        return RedirectToRoute(RouteNames.GetSubscription, new { subscriptionId = subscription.SubscriptionId });
                    }
                    else
                    {
                        this.ApplyLayout(publisherConfig!, User!);

                        return View(nameof(ReserveSeat), model);
                    }
                }
                else
                {
                    // Only the customer -- the subscription admin -- is allowed to reserve seats.
                    // Not even the turnstile admin can reserve seats.

                    return Forbid();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ POST [{nameof(ReserveSeat)}]: [{ex.Message}]");

                throw;
            }
        }

        private string CreateInvitationLink(string subscriptionId, string email) =>
           $"https://{HttpContext.Request.Host}/turnstile/{subscriptionId}?login_hint={email}";
    }
}
