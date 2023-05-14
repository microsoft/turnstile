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
            public const string GetReleaseSeat = "release_seat";
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

                if (publisherConfig?.CheckTurnstileSetupIsComplete(User, logger) is var setupAction &&
                    setupAction != null)
                {
                    return setupAction;
                }
                else
                {
                    ViewData.ApplyModel(new LayoutViewModel(publisherConfig!, User));

                    var subUser = User.ToCoreModel();

                    var subscriptions = (User.CanAdministerTurnstile()
                        ? (await subsClient.GetSubscriptions())
                        : (await subsClient.GetSubscriptions(subUser.TenantId)).Where(s => User.CanAdministerSubscription(s)))
                        .ToList();

                    return View(new SubscriptionsViewModel(subscriptions, User));
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
                else
                {
                    ViewData.ApplyModel(new LayoutViewModel(publisherConfig!, User));

                    var subscription = await subsClient.GetSubscription(subscriptionId);

                    if (subscription == null)
                    {
                        return NotFound();
                    }
                    else if (User.CanAdministerTurnstile() || User.CanAdministerSubscription(subscription))
                    {
                        var seats = await seatsClient.GetSeats(subscriptionId);

                        ViewData.ApplyModel(new SubscriptionContextViewModel(subscription, User));
                        ViewData.ApplyModel(new SubscriptionSeatingViewModel(publisherConfig!, subscription, seats));

                        return View(new SubscriptionDetailViewModel(subscription));
                    }
                    else
                    {
                        return Forbid();
                    }
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
                else
                {
                    ViewData.ApplyModel(new LayoutViewModel(publisherConfig!, User));

                    var subscription = await subsClient.GetSubscription(subscriptionId);

                    if (subscription == null)
                    {
                        return NotFound();
                    }
                    else if (User.CanAdministerSubscription(subscription))
                    {
                        if (ModelState.IsValid)
                        {
                            subscription.ApplyUpdate(subscriptionDetail);

                            subscription = await subsClient.UpdateSubscription(subscription);

                            subscriptionDetail.IsSubscriptionUpdated = true;
                            subscriptionDetail.HasValidationErrors = false;
                        }
                        else
                        {
                            subscriptionDetail.IsSubscriptionUpdated = false;
                            subscriptionDetail.HasValidationErrors = true;
                        }

                        var seats = await seatsClient.GetSeats(subscriptionId);

                        ViewData.ApplyModel(new SubscriptionContextViewModel(subscription!, User));
                        ViewData.ApplyModel(new SubscriptionSeatingViewModel(publisherConfig!, subscription!, seats));

                        return View(subscriptionDetail);
                    }
                    else
                    {
                        return Forbid();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ POST [{nameof(Subscription)}]: [{ex.Message}]");

                throw;
            }
        }

        [HttpGet]
        [Route("subscriptions/{subscriptionId}/seats/{seatId}/release", Name = RouteNames.GetReleaseSeat)]
        public async Task<IActionResult> ReleaseSeat(string subscriptionId, string seatId)
        {
            try
            {
                var publisherConfig = await publisherConfigClient.GetConfiguration();

                if (publisherConfig!.CheckTurnstileSetupIsComplete(User, logger) is var setupAction &&
                    setupAction != null)
                {
                    return setupAction;
                }
                else
                {
                    var subscription = await subsClient.GetSubscription(subscriptionId);
                    var seat = await seatsClient.GetSeat(subscriptionId, seatId);

                    if (subscription == null || seat == null)
                    {
                        return NotFound();
                    }
                    else if (User.CanAdministerSubscription(subscription))
                    {
                        ViewData.ApplyModel(new LayoutViewModel(publisherConfig!, User));
                        ViewData.ApplyModel(new SubscriptionContextViewModel(subscription, User));

                        return View(new SeatViewModel(seat));
                    }
                    else
                    {
                        return Forbid();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ GET [{nameof(ReleaseSeat)}]: [{ex.Message}]");

                throw;
            }
        }

        [HttpPost]
        [Route("subscriptions/{subscriptionId}/seats/{seatId}/release")]
        public async Task<IActionResult> ReleaseSeat(string subscriptionId, string seatId, [FromBody] SeatViewModel model)
        {
            try
            {
                var publisherConfig = await publisherConfigClient.GetConfiguration();

                if (publisherConfig!.CheckTurnstileSetupIsComplete(User, logger) is var setupAction &&
                    setupAction != null)
                {
                    return setupAction;
                }
                else
                {
                    var subscription = await subsClient.GetSubscription(subscriptionId);
                    var seat = await seatsClient.GetSeat(subscriptionId, seatId);

                    if (subscription == null || seat == null)
                    {
                        return NotFound();
                    }
                    else if (User.CanAdministerSubscription(subscription))
                    {
                        await seatsClient.ReleaseSeat(subscriptionId, seatId);

                        return RedirectToRoute(RouteNames.GetSubscription, new { subscriptionId });
                    }
                    else
                    {
                        return Forbid();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ POST [{nameof(ReleaseSeat)}]: [{ex.Message}]");

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
                else
                {
                    var subscription = await subsClient.GetSubscription(subscriptionId);

                    if (subscription?.State != SubscriptionStates.Active)
                    {
                        // Either the subscription doesn't exist or it isn't active.
                        // In either case, you can't reserve a seat in this subscription right now.

                        return NotFound();
                    }
                    else if (User.CanAdministerSubscription(subscription))
                    {
                        ViewData.ApplyModel(new LayoutViewModel(publisherConfig!, User));
                        ViewData.ApplyModel(new SubscriptionContextViewModel(subscription, User));

                        return View(new ReserveSeatViewModel(subscription));
                    }
                    else
                    {
                        return Forbid();
                    }
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
                else
                {
                    var subscription = await subsClient.GetSubscription(subscriptionId);

                    if (subscription == null)
                    {
                        return NotFound();
                    }
                    else if (User.CanAdministerSubscription(subscription))
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

                        ViewData.ApplyModel(new LayoutViewModel(publisherConfig!, User));

                        if (ModelState.IsValid)
                        {
                            return RedirectToRoute(RouteNames.GetSubscription, new { subscriptionId });
                        }
                        else
                        {
                            ViewData.ApplyModel(new SubscriptionContextViewModel(subscription, User));

                            return View(model);
                        }
                    }
                    else
                    {
                        // Only the customer -- the subscription admin -- is allowed to reserve seats.
                        // Not even the turnstile admin can reserve seats.

                        return Forbid();
                    }
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
