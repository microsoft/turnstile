// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Turnstile.Core.Interfaces;
using Turnstile.Web.Admin.Controllers;
using Turnstile.Web.Admin.Extensions;
using Turnstile.Web.Admin.Models;
using Turnstile.Web.Common.Extensions;
using Turnstile.Web.Common.Models;
using Turnstile.Web.Models;

namespace Turnstile.Web.Controllers
{
    [Authorize]
    public class SubscriptionsController : Controller
    {
        public static class RouteNames
        {
            public const string GetSubscription = "subscription";
            public const string GetSubscriptions = "subscriptions";
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
        [Route("/")] // Send admins to subscription list by default.
        [Route("/subscriptions", Name = RouteNames.GetSubscriptions)]
        public async Task<IActionResult> Subscriptions()
        {
            try
            {
                var publisherConfig = await publisherConfigClient.GetConfiguration();

                if (publisherConfig?.IsSetupComplete == true)
                {
                    this.ApplyModel(new LayoutViewModel(publisherConfig!));

                    var subscriptions = (await subsClient.GetSubscriptions()).ToList();

                    return View(new SubscriptionsViewModel(subscriptions, User!, publisherConfig.ClaimsConfiguration!));
                }
                else
                {
                    return RedirectToBasics();
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

                if (publisherConfig?.IsSetupComplete == true)
                {
                    this.ApplyModel(new LayoutViewModel(publisherConfig!));

                    var subscription = await subsClient.GetSubscription(subscriptionId);

                    if (subscription == null)
                    {
                        return NotFound();
                    }
                    else
                    {
                        var seats = await seatsClient.GetSeats(subscriptionId);

                        this.ApplyModel(new SubscriptionContextViewModel(subscription, User!, publisherConfig.ClaimsConfiguration!));
                        this.ApplyModel(new SubscriptionSeatingViewModel(publisherConfig!, subscription, seats));

                        return View(new SubscriptionDetailViewModel(subscription));
                    }
                }
                else
                {
                    return RedirectToBasics();
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
                    this.ApplyModel(new LayoutViewModel(publisherConfig));

                    var subscription = await subsClient.GetSubscription(subscriptionId);

                    if (subscription == null)
                    {
                        return NotFound();
                    }
                    else
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
                }
                else
                {
                    return RedirectToBasics();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ POST [{nameof(Subscription)}]: [{ex.Message}]");

                throw;
            }
        }

        private IActionResult RedirectToBasics() => Redirect(PublisherConfigController.RouteNames.ConfigureBasics);
    }
}
