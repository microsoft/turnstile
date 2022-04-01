using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models.Configuration;
using Turnstile.Web.Extensions;
using Turnstile.Web.Models;

namespace Turnstile.Web.Controllers
{
    [Authorize]
    public class PublisherConfigurationController : Controller
    {
        public static class RouteNames
        {
            public const string GetPublisherConfiguration = "get_publisher_config";
            public const string PutPublisherConfiguration = "put_publisher_config";
        }


        private readonly IPublisherConfigurationClient pubConfigClient;

        public PublisherConfigurationController(IPublisherConfigurationClient pubConfigClient)
        {
            this.pubConfigClient = pubConfigClient;
        }

        [HttpGet]
        [Route("publisher/configuration", Name = "publisher_setup")]
        public async Task<IActionResult> GetPublisherConfig()
        {
            if (User.CanAdministerTurnstile())
            {
                var pubConfig = await pubConfigClient.GetConfiguration();

                if (pubConfig == null)
                {
                    return View(new PublisherConfigurationViewModel());
                }
                else
                {
                    return View(ToViewModel(pubConfig));
                }
            }
            else
            {
                return Forbid();
            }
        }

        [HttpPost]
        [Route("publisher/configuration")]
        public async Task<IActionResult> PostPublisherConfig([FromBody] PublisherConfigurationViewModel pubConfigModel)
        {
            if (User.CanAdministerTurnstile())
            {
                if (ModelState.IsValid)
                {
                    await pubConfigClient.UpdateConfiguration(ToConfigurationModel(pubConfigModel));
                }

                return View(pubConfigModel);
            }
            else
            {
                return Forbid();
            }
        }

        private PublisherConfiguration ToConfigurationModel(PublisherConfigurationViewModel viewModel) =>
            new PublisherConfiguration
            {
                ContactPageUrl = viewModel.ContactPageUrl,
                ContactSalesEmail = viewModel.ContactSalesEmail,
                ContactSalesUrl = viewModel.ContactSalesUrl,
                ContactSupportEmail = viewModel.ContactSupportEmail,
                ContactSupportUrl = viewModel.ContactSupportUrl,
                HomePageUrl = viewModel.HomePageUrl,
                IsSetupComplete = true,
                MonaBaseStorageUrl = viewModel.MonaIntegrationBaseStorageUrl,
                PrivacyNoticePageUrl = viewModel.PrivacyNoticePageUrl,
                PublisherName = viewModel.PublisherName,
                SeatingConfiguration = ToConfigurationModel(viewModel.SeatingConfiguration),
                TurnstileConfiguration = ToConfigurationModel(viewModel.TurnstileConfiguration),
                TurnstileName = viewModel.TurnstileName
            };

        private SeatingConfiguration ToConfigurationModel(SeatingConfigurationViewModel viewModel) =>
           new SeatingConfiguration
           {
               DefaultSeatExpiryInDays = viewModel.SeatExpiryInDays,
               LimitedOverflowSeatingEnabled = viewModel.LimitedOverflowSeatingEnabled,
               LowSeatWarningLevelPercent = (viewModel.LowSeatWarningLevelPercent.GetValueOrDefault() == 0 ? null : viewModel.LowSeatWarningLevelPercent!.Value / 100),
               SeatingStrategyName = viewModel.SeatingStrategyName,
               SeatReservationExpiryInDays = viewModel.SeatReservationExpiryInDays
           };

        private TurnstileConfiguration ToConfigurationModel(TurnstileConfigurationViewModel viewModel) =>
            new TurnstileConfiguration
            {
                OnNoSeatAvailableUrl = viewModel.OnNoSeatsAvailableUrl,
                OnAccessDeniedUrl = viewModel.OnAccessDeniedUrl,
                OnAccessGrantedUrl = viewModel.OnAccessGrantedUrl,
                OnNoSubscriptionsFoundUrl = viewModel.OnNoSubscriptionsFoundUrl,
                OnSubscriptionCanceledUrl = viewModel.OnSubscriptionCanceledUrl,
                OnSubscriptionNotFoundUrl = viewModel.OnSubscriptionNotFoundUrl,
                OnSubscriptionNotReadyUrl = viewModel.OnSubscriptionPurchasedUrl,
                OnSubscriptionSuspendedUrl = viewModel.OnSubscriptionSuspendedUrl
            };

        private PublisherConfigurationViewModel ToViewModel(PublisherConfiguration pubConfig) =>
            new PublisherConfigurationViewModel
            {
                ContactPageUrl = pubConfig.ContactPageUrl,
                ContactSalesEmail = pubConfig.ContactSalesEmail,
                ContactSalesUrl = pubConfig.ContactSalesUrl,
                ContactSupportEmail = pubConfig.ContactSupportEmail,
                ContactSupportUrl = pubConfig.ContactSupportUrl,
                HomePageUrl = pubConfig.HomePageUrl,
                PrivacyNoticePageUrl = pubConfig.PrivacyNoticePageUrl,
                PublisherName = pubConfig.PublisherName,
                TurnstileName = pubConfig.TurnstileName,
                SeatingConfiguration = ToViewModel(pubConfig.SeatingConfiguration ?? new SeatingConfiguration()),
                TurnstileConfiguration = ToViewModel(pubConfig.TurnstileConfiguration ?? new TurnstileConfiguration())
            };

        private SeatingConfigurationViewModel ToViewModel(SeatingConfiguration seatingConfig) =>
            new SeatingConfigurationViewModel
            {
                LimitedOverflowSeatingEnabled = seatingConfig.LimitedOverflowSeatingEnabled,
                LowSeatWarningLevelPercent = seatingConfig.LowSeatWarningLevelPercent.GetValueOrDefault() * 100,
                SeatExpiryInDays = seatingConfig.DefaultSeatExpiryInDays,
                SeatingStrategyName = seatingConfig.SeatingStrategyName,
                SeatReservationExpiryInDays = seatingConfig.SeatReservationExpiryInDays
            };

        private TurnstileConfigurationViewModel ToViewModel(TurnstileConfiguration turnstileConfig) =>
            new TurnstileConfigurationViewModel
            {
                OnNoSeatsAvailableUrl = turnstileConfig.OnNoSeatAvailableUrl,
                OnAccessDeniedUrl = turnstileConfig.OnAccessDeniedUrl,
                OnAccessGrantedUrl = turnstileConfig.OnAccessGrantedUrl,
                OnNoSubscriptionsFoundUrl = turnstileConfig.OnNoSubscriptionsFoundUrl,
                OnSubscriptionCanceledUrl = turnstileConfig.OnSubscriptionCanceledUrl,
                OnSubscriptionNotFoundUrl = turnstileConfig.OnSubscriptionNotFoundUrl,
                OnSubscriptionPurchasedUrl = turnstileConfig.OnSubscriptionNotReadyUrl,
                OnSubscriptionSuspendedUrl = turnstileConfig.OnSubscriptionSuspendedUrl
            };
    }
}
