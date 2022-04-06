using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Turnstile.Core.Constants;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;
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
        public async Task<IActionResult> DefaultTurnstile()
        {
            try
            {
                var publisherConfig = await GetPublisherConfiguration();

                if (publisherConfig!.CheckTurnstileSetupIsComplete(User, logger) is var setupAction &&
                    setupAction != null)
                {
                    // I like this pattern. Common validation is done in an extension method
                    // instead of in an unecessary base class. If the validation method returns an
                    // action, that's the controller's cue to run it.

                    return setupAction;
                }

                var user = User.ToCoreModel();

                var availableSubs = (await subsClient.GetSubscriptions(user.TenantId))
                    .Where(s => s.IsActive() && s.IsSetupComplete == true && User.CanUseSubscription(s))
                    .ToList();

                if (availableSubs.Count == 0)
                {
                    logger.LogWarning($"User [{user.TenantId}/{user.UserId}] has no available subscriptions.");

                    return publisherConfig!.OnNoSubscriptionsFound();
                }
                else if (availableSubs.Count == 1)
                {
                    return RedirectToRoute(
                        RouteNames.SpecificTurnstile,
                        new { subscriptionId = availableSubs[0].SubscriptionId });
                }
                else
                {
                    this.ApplyLayout(publisherConfig!, User!);

                    return View(ViewNames.PickSubscription, new PickSubscriptionModel(availableSubs, User));
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
        public async Task<IActionResult> SpecificTurnstile(string subscriptionId)
        {
            try
            {
                var publisherConfig = await GetPublisherConfiguration();

                if (publisherConfig!.CheckTurnstileSetupIsComplete(User, logger) is var setupAction &&
                    setupAction != null)
                {
                    return setupAction;
                }

                var user = User.ToCoreModel();
                var subscription = await subsClient.GetSubscription(subscriptionId);

                if (subscription == null)
                {
                    return publisherConfig!.OnSubscriptionNotFound(subscriptionId);
                }
                else if (!User.CanUseSubscription(subscription))
                {
                    return publisherConfig!.OnAccessDenied(subscriptionId);
                }
                else if (subscription.State == SubscriptionStates.Canceled)
                {
                    return publisherConfig!.OnSubscriptionCanceled(subscriptionId);
                }
                else if (subscription.State == SubscriptionStates.Suspended)
                {
                    return publisherConfig!.OnSubscriptionSuspended(subscriptionId);
                }
                else
                {
                    var seat = await TryGetSeat(subscription, user);

                    if (seat == null)
                    {
                        return publisherConfig!.OnNoSeatsAvailable(subscriptionId);
                    }
                    else
                    {
                        return publisherConfig!.OnAccessGranted(subscriptionId);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception at [{RouteNames.SpecificTurnstile}]: [{ex.Message}]");

                throw;
            }
        }

        private async Task<Seat?> TryGetSeat(Subscription subscription, User user)
        {
            var seat = await seatsClient.GetSeatByUserId(subscription.SubscriptionId!, user.UserId!);

            if (seat?.Occupant?.UserId == user.UserId)
            {
                // User already has a seat.

                return seat;
            }
            else if (seat?.Reservation?.UserId == user.UserId &&
                     seat?.Reservation?.TenantId == user.TenantId)
            {
                // User has a seat reserved.

                return await TryRedeemSeat(subscription, user, seat!);
            }

            foreach (var userEmail in User.GetEmailAddresses())
            {
                seat = await seatsClient.GetSeatByEmail(subscription.SubscriptionId!, userEmail);

                if (seat != null)
                {
                    // User has a seat reserved by their email.

                    return await TryRedeemSeat(subscription, user, seat!);
                }
            }

            return await seatsClient.RequestSeat(subscription.SubscriptionId!, user);
        }

        private async Task<Seat> TryRedeemSeat(Subscription subscription, User user, Seat seat)
        {
            var redeemedSeat = await seatsClient.RedeemSeat(subscription.SubscriptionId!, user, seat!.SeatId!);

            if (seat == null)
            {
                throw new Exception(
                    $"Unable to redeem seat [{seat!.SeatId}] reserved " +
                    $"for user [{user.TenantId}/{user.UserId}].");
            }
            else
            {
                return seat;
            }
        }

        [HttpGet]
        [Route("turnstile/on/no-subscriptions", Name = RouteNames.OnNoSubscriptions)]
        public async Task<IActionResult> OnNoSubscriptions()
        {
            try
            {
                var pubConfig = await GetPublisherConfiguration();
                var messageModel = new SubscriptionMessageViewModel(pubConfig!);

                this.ApplyLayout(pubConfig!, User!);

                return View(messageModel);
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
                var pubConfig = await GetPublisherConfiguration();
                var subscription = await subsClient.GetSubscription(subscriptionId);

                if (subscription == null)
                {
                    return RedirectToRoute(RouteNames.OnSubscriptionNotFound, new { subscriptionId = subscriptionId });
                }

                var subUser = User.ToCoreModel();

                var isTenantAdmin =
                    User.CanAdministerAllTenantSubscriptions(subUser.TenantId!) ||
                    User.CanAdministerSubscription(subscription);

                var messageModel = new SubscriptionMessageViewModel(pubConfig!, subscription, isTenantAdmin);

                this.ApplyLayout(pubConfig!, User!);

                return View(messageModel);
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ [{nameof(OnSubscriptionCanceled)}]: [{ex.Message}]");

                throw;
            }
        }

        [HttpGet]
        [Route("turnstile/on/subscription-suspended/{subscriptionId}", Name = RouteNames.OnSubscriptionSuspended)]
        public async Task<IActionResult> OnSubscriptionSuspended(string subscriptionId)
        {
            try
            {
                var pubConfig = await GetPublisherConfiguration();
                var subscription = await subsClient.GetSubscription(subscriptionId);

                if (subscription == null)
                {
                    return RedirectToRoute(RouteNames.OnSubscriptionNotFound, new { subscriptionId = subscriptionId });
                }

                var subUser = User.ToCoreModel();

                var isTenantAdmin =
                    User.CanAdministerAllTenantSubscriptions(subUser.TenantId!) ||
                    User.CanAdministerSubscription(subscription);

                this.ApplyLayout(pubConfig!, User!);

                var messageModel = new SubscriptionMessageViewModel(pubConfig!, subscription, isTenantAdmin);

                return View(messageModel);
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
                var pubConfig = await GetPublisherConfiguration();
                var subUser = User.ToCoreModel();
                var isTenantAdmin = User.CanAdministerAllTenantSubscriptions(subUser.TenantId!);
                var messageModel = new SubscriptionMessageViewModel(pubConfig!, isTenantAdmin);

                this.ApplyLayout(pubConfig!, User!);

                return View(messageModel);
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
                var pubConfig = await GetPublisherConfiguration();
                var subscription = await subsClient.GetSubscription(subscriptionId);

                if (subscription == null)
                {
                    return RedirectToRoute(RouteNames.OnSubscriptionNotFound, new { subscriptionId = subscriptionId });
                }

                var subUser = User.ToCoreModel();

                var isTenantAdmin =
                    User.CanAdministerAllTenantSubscriptions(subUser.TenantId!) ||
                    User.CanAdministerSubscription(subscription);

                var messageModel = new SubscriptionMessageViewModel(pubConfig!, subscription, isTenantAdmin);

                this.ApplyLayout(pubConfig!, User!);

                return View(messageModel);
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception @ [{nameof(OnNoSeatsAvailable)}]: [{ex.Message}]");

                throw;
            }
        }
    }
}
