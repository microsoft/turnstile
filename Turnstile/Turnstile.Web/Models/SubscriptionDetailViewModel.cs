// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Turnstile.Core.Constants;
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
            TenantId = subscription.TenantId;
            TenantName = subscription.TenantName;
            State = subscription.State;
            OfferId = subscription.OfferId;
            PlanId = subscription.PlanId;
            AdminRoleName = subscription.AdminRoleName;
            UserRoleName = subscription.UserRoleName;
            AdminName = subscription.AdminName;
            AdminEmail = subscription.AdminEmail;
            IsBeingConfigured = (subscription.IsBeingConfigured == true);

            AvailableStates = new List<SelectListItem>(GetAvailableStates(subscription));
        }

        [Display(Name = "Subscription ID")]
        public string? SubscriptionId { get; set; }

        [Display(Name = "Subscription name")]
        public string? SubscriptionName { get; set; }

        [Display(Name = "Country")]
        [Required(ErrorMessage = "Country is required.")]
        public string? TenantCountry { get; set; }

        [Display(Name = "Tenant ID")]
        public string? TenantId { get; set; }

        [Display(Name = "Tenant name")]
        [Required(ErrorMessage = "Tenant name is required.")]
        public string? TenantName { get; set; }

        [Display(Name = "State")]
        public string? State { get; set; }

        [Display(Name = "Offer ID")]
        [Required(ErrorMessage = "Offer ID is required.")]
        public string? OfferId { get; set; }

        [Display(Name = "Plan ID")]
        [Required(ErrorMessage = "Plan ID is required.")]
        public string? PlanId { get; set; }

        [Display(Name = "Subscription admin role name")]
        public string? AdminRoleName { get; set; }

        [Display(Name = "Subscription user role name")]
        public string? UserRoleName { get; set; }

        [Display(Name = "Primary admin name")]
        public string? AdminName { get; set; }

        [Display(Name = "Primary admin email")]
        [EmailAddress(ErrorMessage = "Subscription admin email is not a valid email address.")]
        public string? AdminEmail { get; set; }

        [Display(Name = "Subscription is currently being configured")]
        public bool IsBeingConfigured { get; set; }

        public bool IsSubscriptionUpdated { get; set; }
        public bool HasValidationErrors { get; set; }

        public List<SelectListItem> AvailableStates { get; set; } = new List<SelectListItem>();

        // TODO: This probably isn't the most reliable way of getting country names. Revisit...

        public List<SelectListItem> Countries { get; set; } = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Select(ci => new RegionInfo(ci.LCID))
            .Select(ri => new { ri.EnglishName, ri.TwoLetterISORegionName })
            .DistinctBy(c => c.EnglishName)
            .OrderBy(c => c.EnglishName)
            .Select(c => new SelectListItem(c.EnglishName, c.TwoLetterISORegionName))
            .ToList();

        private IEnumerable<SelectListItem> GetAvailableStates(Subscription subscription) => subscription.State switch
        {
            SubscriptionStates.Purchased => new[] { GetPurchasedStateListItem(true), GetActiveStateListItem(), GetCanceledStateListItem(), GetSuspendedStateListItem() },
            SubscriptionStates.Active => new[] { GetActiveStateListItem(true), GetCanceledStateListItem(), GetSuspendedStateListItem() },
            SubscriptionStates.Canceled => new[] { GetCanceledStateListItem(true) },
            SubscriptionStates.Suspended => new[] { GetActiveStateListItem(), GetCanceledStateListItem(), GetSuspendedStateListItem(true) },
            _ => throw new ArgumentOutOfRangeException($"Subscription state [{subscription.State}] is invalid.")
        };

        private SelectListItem GetPurchasedStateListItem(bool inState = false) =>
            new SelectListItem("Purchased", SubscriptionStates.Purchased, inState);

        private SelectListItem GetActiveStateListItem(bool inState = false) =>
            new SelectListItem("Active", SubscriptionStates.Active, inState);

        private SelectListItem GetCanceledStateListItem(bool inState = false) =>
            new SelectListItem("Canceled", SubscriptionStates.Canceled, inState);

        private SelectListItem GetSuspendedStateListItem(bool inState = false) =>
            new SelectListItem("Suspended", SubscriptionStates.Suspended, inState);
    }
}
