using Microsoft.AspNetCore.Mvc;
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
            public const string GetSubscription = "get_subscription";
            public const string GetSubscriptions = "get_subscriptions";
            public const string GetReserveSeat = "get_reserve_seat";
            public const string PostReserveSeat = "post_reserve_seat";
            public const string GetSubscriptionSetup = "get_subscription_setup";
            public const string PostSubscriptionSetup = "post_subscription_setup";
        }

        public static class SortableFields
        {
            public const string SubscriptionName = "subscription_name";
            public const string TenantName = "tenant_name";
            public const string State = "state";
            public const string OfferId = "offer_id";
            public const string PlanId = "plan_id";
            public const string SeatingStrategy = "seating_strategy";
            public const string TotalSeats = "total_seats";
            public const string CreatedDate = "created";
        }

        public static IEnumerable<Subscription> Sort(IEnumerable<Subscription> subscriptions, string fieldName) =>
            fieldName switch
            {
                SortableFields.TenantName => subscriptions.OrderBy(s => s.TenantName ?? s.TenantId),
                SortableFields.State => subscriptions.OrderBy(s => s.State),
                SortableFields.OfferId => subscriptions.OrderBy(s => s.OfferId),
                SortableFields.PlanId => subscriptions.OrderBy(s => s.PlanId),
                SortableFields.SeatingStrategy => subscriptions.OrderBy(s => s.SeatingConfiguration!.SeatingStrategyName),
                SortableFields.TotalSeats => subscriptions.OrderBy(s => s.TotalSeats),
                SortableFields.CreatedDate => subscriptions.OrderBy(s => s.CreatedDateTimeUtc),
                _ => subscriptions.OrderBy(s => s.SubscriptionName)
            };

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
                else if (User.CanAdministerAllTenantSubscriptions())
                {
                    subscriptions.AddRange(await subsClient.GetSubscriptions(subUser.TenantId!));
                }
                else
                {
                    return Forbid();
                }

                sort ??= SortableFields.SubscriptionName;

                return View(new SubscriptionsViewModel(Sort(subscriptions, sort)));
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

                var isTurnstileAdmin = User.CanAdministerTurnstile(); // Publisher admin

                var isSubscriberAdmin = // Subscriber admin
                    User.CanAdministerAllTenantSubscriptions(subscription.TenantId!) ||
                    User.CanAdministerSubscription(subscription);

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

        [HttpGet]
        [Route("subscriptions/{subscriptionId}/seats/reserve", Name = RouteNames.GetReserveSeat)]
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

                var isSubscriberAdmin = // Subscriber admin
                    User.CanAdministerAllTenantSubscriptions(subscription.TenantId!) ||
                    User.CanAdministerSubscription(subscription);

                if (isSubscriberAdmin)
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
        [Route("subscriptions/{subscriptionId}/seats/reserve", Name = RouteNames.PostReserveSeat)]
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

                var isSubscriberAdmin = // Subscriber admin
                    User.CanAdministerAllTenantSubscriptions(subscription.TenantId!) ||
                    User.CanAdministerSubscription(subscription);

                if (isSubscriberAdmin)
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
                            var seat = await seatsClient.ReserveSeat(subscriptionId, new Reservation { Email = model.ForEmail! });

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
                    // Only the customer -- the subscriber admin -- is allowed to reserve seats.
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


        [HttpGet]
        [Route("subscriptions/{subscriptionId}/setup", Name = RouteNames.GetSubscriptionSetup)]
        public async Task<IActionResult> SubscriptionSetup(string subscriptionId)
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

                this.ApplyLayout(publisherConfig!, User!);

                var setupModel = new SubscriptionSetupViewModel(publisherConfig!, subscription, User!);

                return View(setupModel);
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ GET [{nameof(SubscriptionSetup)}]: [{ex.Message}");

                throw;
            }
        }

        [HttpPost]
        [Route("subscriptions/{subscriptionId}/setup", Name = RouteNames.PostSubscriptionSetup)]
        public async Task<IActionResult> SubscriptionSetup(string subscriptionId, [FromForm] SubscriptionSetupViewModel setupModel)
        {
            try
            {
                var publisherConfig = await publisherConfigClient.GetConfiguration();

                if (publisherConfig!.CheckTurnstileSetupIsComplete(User, logger) is var setupAction &&
                    setupAction != null)
                {
                    return setupAction;
                }

                if (ModelState.IsValid)
                {
                    var subscription = await subsClient.GetSubscription(subscriptionId);

                    if (subscription == null)
                    {
                        return publisherConfig!.OnSubscriptionNotFound(subscriptionId);
                    }

                    var patch = setupModel.CreatePatch();

                    patch.IsSetupComplete = true;

                    await subsClient.UpdateSubscription(patch);

                    return RedirectToRoute(RouteNames.GetSubscription, new { subscriptionId = subscriptionId });
                }
                else
                {
                    this.ApplyLayout(publisherConfig!, User!);

                    return View(setupModel);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ POST [{nameof(SubscriptionSetup)}]: [{ex.Message}");

                throw;
            }
        }
    }
}
