namespace Turnstile.Web.Models
{
    public class SubscriptionSetupViewModel
    {
        public string? SubscriptionId { get; set; }

        public SubscriberInfoViewModel? SubscriberInfo { get; set; }

        public SubscriptionConfigurationViewModel? SubscriptionConfiguration { get; set; }
    }
}
