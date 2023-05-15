// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text.Json;
using Turnstile.Core.Constants;
using Turnstile.Core.Extensions;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Configuration;
using Turnstile.Web.Extensions;

namespace Turnstile.Web.Controllers
{
    [Authorize]
    public class MonaIntegrationController : Controller
    {
        private readonly HttpClient httpClient;
        private readonly ILogger logger;
        private readonly IPublisherConfigurationClient pubConfigClient;
        private readonly ISubscriptionsClient subsClient;

        public MonaIntegrationController(
            HttpClient httpClient,
            ILogger<MonaIntegrationController> logger,
            IPublisherConfigurationClient pubConfigClient,
            ISubscriptionsClient subsClient)
        {
            this.httpClient = httpClient;
            this.logger = logger;
            this.pubConfigClient = pubConfigClient;
            this.subsClient = subsClient;
        } 

        [HttpGet]
        [Route("from-mona", Name = "from_mona")]
        public async Task<IActionResult> Index(string? _sub = null)
        {
            try
            {
                var pubConfig = await pubConfigClient.GetConfiguration();

                if (pubConfig == null || !pubConfig!.IsSetupComplete)
                {
                    logger.LogWarning("Publisher setup is incomplete.");

                    return User!.CanAdministerTurnstile() ? RedirectToRoute("get_publisher_config") : this.ServiceUnavailable();
                }

                if (string.IsNullOrEmpty(pubConfig!.MonaBaseStorageUrl))
                {
                    logger.LogWarning("Mona base storage URL [mona_base_storage_url] not configured.");

                    return this.ServiceUnavailable();
                }

                if (string.IsNullOrEmpty(_sub))
                {
                    logger.LogWarning($"Mona SAS fragment query parameter [{nameof(_sub)}] is missing.");

                    return BadRequest();
                }

                var monaSubscription = await GetMonaSubscription(pubConfig, _sub);

                if (monaSubscription == null)
                {
                    logger.LogWarning($"Unable to get Mona subscription from blob storage.");

                    return NotFound();
                }

                var validationErrors = monaSubscription.Validate();

                if (validationErrors.Any())
                {
                    logger.LogWarning($"Provided Mona subscription is invalid: {validationErrors.ToParagraph()}");

                    return BadRequest();
                }

                if (User!.GetHomeTenantId() != monaSubscription.Beneficiary!.AadTenantId)
                {
                    logger.LogWarning(
                        $"User is trying to create a subscription [{monaSubscription.SubscriptionId}] in a tenant they don't belong to " +
                        $"[{monaSubscription.Beneficiary!.AadTenantId}]. Operation is forbidden.");

                    return Forbid();
                }

                var subscription = ToCoreSubscription(monaSubscription, pubConfig);

                await subsClient.CreateSubscription(subscription);

                return RedirectToRoute(SubscriptionsController.RouteNames.GetSubscription, new { subscriptionId = subscription.SubscriptionId });
            }
            catch (Exception ex)
            {
                logger.LogError($"An error occurred while trying to import an existing Mona subscription: [{ex.Message}].");

                throw;
            }
        }

        private Subscription ToCoreSubscription(Models.Mona.Subscription monaSubscription, PublisherConfiguration publisherConfig) =>
           new Subscription
           {
               CreatedDateTimeUtc = DateTime.UtcNow,
               IsFreeTrial = monaSubscription.IsFreeTrial.GetValueOrDefault(),
               IsTestSubscription = monaSubscription.IsTest.GetValueOrDefault(),
               IsSetupComplete = false,
               ManagementUrls = new Dictionary<string, string>
               {
                   // This is kind of a hack right now but it's a necessary one. Per the Marketplace docs, 
                   // while publishers _can_ request cancelations, plan changes, and seat quantity changes 
                   // from the SaaS app, ideally they'd be redirected to their subscription in the Azure portal instead.
                   // This also makes it possible to support subscriptions that are not managed by Azure Marketplace/AppSource.
                   // FEEDBACK: There should be a way to get a "management URL" for Marketplace subscriptions.

                   ["Manage your Azure Marketplace subscriptions"] = "https://portal.azure.com/#blade/HubsExtension/BrowseResourceBlade/resourceType/Microsoft.SaaS%2Fresources"
               },
               OfferId = monaSubscription.OfferId,
               PlanId = monaSubscription.PlanId,
               SourceSubscription = JObject.FromObject(monaSubscription),
               State = (publisherConfig.DefaultMonaSubscriptionState ?? ToCoreState(monaSubscription.Status)),
               IsBeingConfigured = publisherConfig.MonaSubscriptionIsBeingConfigured,
               SubscriptionId = monaSubscription.SubscriptionId,
               SubscriptionName = monaSubscription.SubscriptionName,
               TenantId = monaSubscription.Beneficiary?.AadTenantId,
               StateLastUpdatedDateTimeUtc = DateTime.UtcNow,
               TotalSeats = monaSubscription.SeatQuantity
           };

        private string ToCoreState(Models.Mona.SubscriptionStatus subStatus) =>
            subStatus switch
            {
                Models.Mona.SubscriptionStatus.PendingConfirmation => SubscriptionStates.Purchased,
                Models.Mona.SubscriptionStatus.PendingActivation => SubscriptionStates.Purchased,
                Models.Mona.SubscriptionStatus.Cancelled => SubscriptionStates.Canceled,
                Models.Mona.SubscriptionStatus.Suspended => SubscriptionStates.Suspended,
                _ => throw new ArgumentException($"Unable to handle Mona [{subStatus}] subscription status.", nameof(subStatus))
            };

        private async Task<Models.Mona.Subscription?> GetMonaSubscription(PublisherConfiguration pubConfig, string _sub)
        {
            var httpResponse = await httpClient.GetAsync(new Uri(new Uri(pubConfig.MonaBaseStorageUrl!), _sub));

            if (httpResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            else
            {
                httpResponse.EnsureSuccessStatusCode();

                return await JsonSerializer.DeserializeAsync<Models.Mona.Subscription>(httpResponse.Content.ReadAsStream());
            }
        }
    }
}
