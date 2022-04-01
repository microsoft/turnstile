using Microsoft.AspNetCore.Mvc;
using Turnstile.Core.Interfaces;

namespace Turnstile.Web.Controllers
{
    public class SubscriptionsController : Controller
    {
        public static class RouteNames
        {
            public const string GetSubscriptions = "get_subscriptions";
        }

        private readonly ILogger logger;
        private readonly IPublisherConfigurationClient publisherConfigClient;
        private readonly ISubscriptionsClient subsClient;

        public SubscriptionsController(
            ILogger<SubscriptionsController> logger,
            IPublisherConfigurationClient publisherConfigClient,
            ISubscriptionsClient subsClient)
        {
            this.logger = logger;
            this.publisherConfigClient = publisherConfigClient;
            this.subsClient = subsClient;
        }

        [HttpGet]
        [Route("subscriptions", Name = RouteNames.GetSubscriptions)]
        public IActionResult Index()
        {
            return View();
        }
    }
}
