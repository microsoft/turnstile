using System.ComponentModel.DataAnnotations;
using Turnstile.Core.Models;

namespace Turnstile.Web.Models
{
    public class ReserveSeatViewModel
    {
        public ReserveSeatViewModel() { }

        public ReserveSeatViewModel(Subscription subscription)
        {
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));

            SubscriptionId = subscription.SubscriptionId;
            SubscriptionName = subscription.SubscriptionName;
        }

        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Email address is invalid.")]
        [Required(ErrorMessage = "Email address is required.")]
        public string? ForEmail { get; set; }

        public string? SubscriptionId { get; set; }
        public string? SubscriptionName { get; set; }
    }
}
