using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Turnstile.Web.Models
{
    public class SubscriberInfoViewModel
    {
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
            .Select(ci => new RegionInfo(ci.LCID).EnglishName)
            .Distinct()
            .OrderBy(rn => rn)
            .Select(rn => new SelectListItem(rn, rn))
            .ToList();
    }
}
