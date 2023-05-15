namespace Turnstile.Web.Models.PublisherConfig
{
    public abstract class BaseConfigurationViewModel
    {
        public bool IsConfigurationSaved { get; set; } = false;
        public bool HasValidationErrors { get; set; } = false;
    }
}
