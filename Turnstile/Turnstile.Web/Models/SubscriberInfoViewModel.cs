using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using Turnstile.Core.Models;

namespace Turnstile.Web.Models
{
    public class SubscriberInfoViewModel
    {
        public SubscriberInfoViewModel() { }

        public SubscriberInfoViewModel(Subscription subscription, ClaimsPrincipal forPrincipal)
        {
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));
            ArgumentNullException.ThrowIfNull(forPrincipal, nameof(forPrincipal));

            var subscriberInfo = subscription.SubscriberInfo == null ?
                new SubscriberInfo() :
                subscription.SubscriberInfo.ToObject<SubscriberInfo>();

            var thisCountry = 
                forPrincipal.FindFirstValue(ClaimTypes.Country) ?? 
                RegionInfo.CurrentRegion.TwoLetterISORegionName;

            TenantName = 
                subscription.TenantName ?? 
                forPrincipal.FindFirstValue("companyName") ??
                subscription.SubscriptionName;

            TenantCountry = subscriberInfo!.TenantCountry ?? thisCountry;
            AdminName = subscription.AdminName ?? forPrincipal.FindFirstValue(ClaimTypes.Name);
            AdminEmail = subscription.AdminEmail ?? forPrincipal.FindFirstValue(ClaimTypes.Email);
        }

        public Subscription ApplyTo(Subscription subscription)
        {
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));

            subscription.TenantName = TenantName;
            subscription.AdminName = AdminName;
            subscription.AdminEmail = AdminEmail;

            var subscriberInfo = subscription.SubscriberInfo == null ?
                new SubscriberInfo() :
                subscription.SubscriberInfo.ToObject<SubscriberInfo>();

            subscriberInfo!.TenantCountry = TenantCountry;

            subscription.SubscriberInfo = JObject.FromObject(subscriberInfo);

            return subscription;
        }

        [Display(Name = "Tenant name")]
        [Required(ErrorMessage = "Tenant name is required.")]
        public string? TenantName { get; set; }

        [Display(Name = "Tenant country")]
        [Required(ErrorMessage = "Tenant country is required.")]
        public string? TenantCountry { get; set; }

        [Display(Name = "Administrator name")]
        public string? AdminName { get; set; }

        [Display(Name = "Administrator email")]
        [Required(ErrorMessage = "Administrator email is required.")]
        [EmailAddress(ErrorMessage = "Administrator email is invalid.")]
        public string? AdminEmail { get; set; }

        // TODO: This probably isn't the most reliable way of getting country names. Revisit...

        public List<SelectListItem> Countries { get; set; } = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Select(ci => new RegionInfo(ci.LCID))
            .Select(ri => new { ri.EnglishName, ri.TwoLetterISORegionName})
            .DistinctBy(c => c.EnglishName)
            .OrderBy(c => c.EnglishName)
            .Select(c => new SelectListItem(c.EnglishName, c.TwoLetterISORegionName))
            .ToList();
    }
}
