// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Turnstile.Core.Models;

namespace Turnstile.Web.Models
{
    public class SubscriptionDetailViewModel
    {
        public SubscriptionDetailViewModel() { }

        public SubscriptionDetailViewModel(Subscription subscription)
        {
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));

            SubscriptionId = subscription.SubscriptionId;
            SubscriptionName = subscription.SubscriptionName;
            TenantName = subscription.TenantName;
            AdminRoleName = subscription.AdminRoleName;
            UserRoleName = subscription.UserRoleName;
            AdminName = subscription.AdminName;
            AdminEmail = subscription.AdminEmail;

            var subscriberInfo = subscription.SubscriberInfo == null
                ? new SubscriberInfo()
                : subscription.SubscriberInfo.ToObject<SubscriberInfo>();

            var thisCountry = RegionInfo.CurrentRegion.TwoLetterISORegionName;

            TenantCountry = subscriberInfo!.TenantCountry ?? thisCountry;
        }

        [Display(Name = "Subscription ID")]
        public string? SubscriptionId { get; set; }

        [Display(Name = "Subscription name")]
        public string? SubscriptionName { get; set; }

        [Display(Name = "Country")]
        [Required(ErrorMessage = "Country is required.")]
        public string? TenantCountry { get; set; }

        [Display(Name = "Tenant name")]
        [Required(ErrorMessage = "Tenant name is required.")]
        public string? TenantName { get; set; }

        [Display(Name = "Subscription admin role name")]
        public string? AdminRoleName { get; set; }

        [Display(Name = "Subscription user role name")]
        public string? UserRoleName { get; set; }

        [Display(Name = "Primary admin name")]
        [Required(ErrorMessage = "Primary admin name is required.")]
        public string? AdminName { get; set; }

        [Display(Name = "Primary admin email")]
        [Required(ErrorMessage = "Primary admin email is required.")]
        [EmailAddress(ErrorMessage = "Subscription admin email is not a valid email address.")]
        public string? AdminEmail { get; set; }

        public bool IsSubscriptionUpdated { get; set; }
        public bool HasValidationErrors { get; set; }

        public List<SelectListItem> Countries { get; set; } = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Select(ci => new RegionInfo(ci.LCID))
            .Select(ri => new { ri.EnglishName, ri.TwoLetterISORegionName })
            .DistinctBy(c => c.EnglishName)
            .OrderBy(c => c.EnglishName)
            .Select(c => new SelectListItem(c.EnglishName, c.TwoLetterISORegionName))
            .ToList();
    }
}
