using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Turnstile.Core.Interfaces;
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
            public const string PostPublisherConfiguration = "post_publisher_config";
        }

        private readonly IPublisherConfigurationClient pubConfigClient;

        public PublisherConfigurationController(IPublisherConfigurationClient pubConfigClient)
        {
            this.pubConfigClient = pubConfigClient;
        }

        [HttpGet]
        [Route("publisher/setup", Name = RouteNames.GetPublisherConfiguration)]
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
                    return View(new PublisherConfigurationViewModel(pubConfig));
                }
            }
            else
            {
                return Forbid();
            }
        }

        [HttpPost]
        [Route("publisher/setup")]
        public async Task<IActionResult> PostPublisherConfig([FromForm] PublisherConfigurationViewModel pubConfigModel)
        {
            if (User.CanAdministerTurnstile())
            {
                if (ModelState.IsValid)
                {
                    await pubConfigClient.UpdateConfiguration(pubConfigModel.ToCoreModel());
                }

                return View(nameof(GetPublisherConfig), pubConfigModel);
            }
            else
            {
                return Forbid();
            }
        }    
    }
}
