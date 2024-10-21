using Microsoft.AspNetCore.Mvc;
using Turnstile.Core.Interfaces;
using Turnstile.Web.Admin.Models;

namespace Turnstile.Web.Admin.Controllers
{
    public class MeController : Controller
    {
        private readonly ILogger log;
        private readonly IPublisherConfigurationClient publisherConfigClient;

        public MeController(
            ILogger<MeController> log,
            IPublisherConfigurationClient publisherConfigClient)
        {
            this.log = log;
            this.publisherConfigClient = publisherConfigClient;
        }

        [HttpGet, Route("me", Name = "me")]
        public async Task<IActionResult> Me()
        {
            try
            {
                var publisherConfig = await publisherConfigClient.GetConfiguration();

                if (publisherConfig?.IsSetupComplete == true)
                {
                    return View(new MeModel(User, publisherConfig.ClaimsConfiguration!));
                }
                else
                {
                    return RedirectToRoute(PublisherConfigController.RouteNames.ConfigureBasics);
                }   
            }
            catch (Exception ex)
            {
                log.LogError($"Exception @ GET [{nameof(Me)}]: [{ex.Message}].");

                throw;
            }
        }
    }
}
