// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Turnstile.Core.Constants;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;
using Turnstile.Web.Common.Extensions;
using Turnstile.Web.Common.Models;
using Turnstile.Web.Extensions;
using Turnstile.Web.Models;

namespace Turnstile.Web.Controllers
{
    [Authorize]
    public class SubscriptionsController : Controller
    {
        public static class RouteNames
        {
            public const string GetSubscription = "subscription";
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
        [Route("subscriptions/{subscriptionId}", Name = RouteNames.GetSubscription)]
        public async Task<IActionResult> Subscription(string subscriptionId)
        {
            try
            {
                var publisherConfig = await publisherConfigClient.GetConfiguration();

                if (publisherConfig?.IsSetupComplete == true)
                {
                    this.ApplyModel(new LayoutViewModel(publisherConfig));

                    var subscription = await subsClient.GetSubscription(subscriptionId);

                    if (subscription == null)
                    {
                        return NotFound();
                    }
                    else if (User.CanAdministerSubscription(publisherConfig.ClaimsConfiguration!, subscription))
                    {
                        var seats = await seatsClient.GetSeats(subscriptionId);

                        this.ApplyModel(new SubscriptionContextViewModel(subscription, User!, publisherConfig.ClaimsConfiguration!));
                        this.ApplyModel(new SubscriptionSeatingViewModel(publisherConfig!, subscription, seats));

                        return View(new SubscriptionDetailViewModel(subscription));
                    }
                    else
                    {
                        return Forbid();
                    }
                }
                else
                {
                    return this.ServiceUnavailable();
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
        public async Task<IActionResult> Subscription(string subscriptionId, [FromForm] SubscriptionDetailViewModel subscriptionDetail)
        {
            try
            {
                var publisherConfig = await publisherConfigClient.GetConfiguration();

                if (publisherConfig?.IsSetupComplete == true)
                {
                    this.ApplyModel(new LayoutViewModel(publisherConfig!));

                    var subscription = await subsClient.GetSubscription(subscriptionId);

                    if (subscription == null)
                    {
                        return NotFound();
                    }
                    else if (User.CanAdministerSubscription(publisherConfig.ClaimsConfiguration!, subscription))
                    {
                        if (ModelState.IsValid)
                        {
                            subscription.ApplyUpdate(subscriptionDetail);

                            subscription.IsSetupComplete = true;

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

                        this.ApplyModel(new SubscriptionContextViewModel(subscription!, User!, publisherConfig.ClaimsConfiguration!));
                        this.ApplyModel(new SubscriptionSeatingViewModel(publisherConfig!, subscription!, seats));

                        return View(subscriptionDetail);
                    }
                    else
                    {
                        return Forbid();
                    }
                }
                else
                {
                    return this.ServiceUnavailable();
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

                if (publisherConfig?.IsSetupComplete == true)
                {
                    var subscription = await subsClient.GetSubscription(subscriptionId);
                    var seat = await seatsClient.GetSeat(subscriptionId, seatId);

                    if (subscription == null || seat == null)
                    {
                        return NotFound();
                    }
                    else if (User.CanAdministerSubscription(publisherConfig.ClaimsConfiguration!, subscription)) 
                        // Only the subscriber themselves can release a seat in their subscription.
                        // This decision is up for debate since, technically, the publisher can always
                        // go in and release a seat. Points to the need for a complete RACI of all the folks
                        // that will wind up using Turnstile -- publishers, subscribers, and users.
                    {
                        this.ApplyModel(new LayoutViewModel(publisherConfig!));
                        this.ApplyModel(new SubscriptionContextViewModel(subscription, User!, publisherConfig.ClaimsConfiguration!));

                        return View(new SeatViewModel(seat));
                    }
                    else
                    {
                        return Forbid();
                    }
                }
                else
                {
                    return this.ServiceUnavailable();
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
        public async Task<IActionResult> ReleaseSeat(string subscriptionId, string seatId, [FromForm] SeatViewModel model)
        {
            try
            {
                var publisherConfig = await publisherConfigClient.GetConfiguration();

                if (publisherConfig?.IsSetupComplete == true)
                {
                    var subscription = await subsClient.GetSubscription(subscriptionId);
                    var seat = await seatsClient.GetSeat(subscriptionId, seatId);

                    if (subscription == null || seat == null)
                    {
                        return NotFound();
                    }
                    else if (User.CanAdministerSubscription(publisherConfig.ClaimsConfiguration!, subscription))
                    {
                        await seatsClient.ReleaseSeat(subscriptionId, seatId);

                        return RedirectToRoute(RouteNames.GetSubscription, new { subscriptionId });
                    }
                    else
                    {
                        return Forbid();
                    }
                }
                else
                {
                    return this.ServiceUnavailable();
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

                if (publisherConfig?.IsSetupComplete == true)
                { 
                    var subscription = await subsClient.GetSubscription(subscriptionId);

                    if (subscription?.State != SubscriptionStates.Active)
                    {
                        // Either the subscription doesn't exist or it isn't active.
                        // In either case, you can't reserve a seat in this subscription right now.

                        return NotFound();
                    }
                    else if (User.CanAdministerSubscription(publisherConfig.ClaimsConfiguration!, subscription)) 
                        // Same rule as with releasing seats. Only subscribers can
                        // can create seat reservations through the UI.
                    {
                        this.ApplyModel(new LayoutViewModel(publisherConfig!));
                        this.ApplyModel(new SubscriptionContextViewModel(subscription, User!, publisherConfig.ClaimsConfiguration!));

                        return View(new ReserveSeatViewModel(subscription));
                    }
                    else
                    {
                        return Forbid();
                    }
                }
                else
                {
                    return this.ServiceUnavailable();
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

                if (publisherConfig?.IsSetupComplete == true)
                {
                    var subscription = await subsClient.GetSubscription(subscriptionId);

                    if (subscription?.State != SubscriptionStates.Active)
                    {
                        return NotFound();
                    }
                    else if (User.CanAdministerSubscription(publisherConfig.ClaimsConfiguration!, subscription))
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

                        this.ApplyModel(new LayoutViewModel(publisherConfig!));

                        if (ModelState.IsValid)
                        {
                            return RedirectToRoute(RouteNames.GetSubscription, new { subscriptionId });
                        }
                        else
                        {
                            this.ApplyModel(new SubscriptionContextViewModel(subscription, User!, publisherConfig.ClaimsConfiguration!));

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
                else
                {
                    return this.ServiceUnavailable();
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
