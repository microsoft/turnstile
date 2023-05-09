// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Turnstile.Core.Constants;
using Turnstile.Core.Models;

namespace Turnstile.Web.Models
{
    public class SubscriptionDetailViewModel
    {
        public SubscriptionDetailViewModel() { }

        public SubscriptionDetailViewModel(Subscription subscription, IEnumerable<Seat> seats, 
            bool userIsTurnstileAdmin = false, 
            bool userIsSubscriberAdmin = false)
        {
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));
            ArgumentNullException.ThrowIfNull(seats, nameof(seats));

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

            IsBeingConfigured = subscription.IsBeingConfigured == true;
            IsTestSubscription = subscription.IsTestSubscription;
            IsFreeSubscription = subscription.IsFreeTrial;
            UserIsTurnstileAdmin = userIsTurnstileAdmin;
            UserIsSubscriberAdmin = userIsSubscriberAdmin;

            CreatedDateTimeUtc = subscription.CreatedDateTimeUtc;
            StateLastUpdatedDateTimeUtc = subscription.StateLastUpdatedDateTimeUtc;

            ManagementUrls = subscription.ManagementUrls;

            AvailableStates = new List<SelectListItem>(GetAvailableStates(subscription));
        }

        [Display(Name = "Subscription ID")]
        public string? SubscriptionId { get; set; }

        [Display(Name = "Subscription name")]
        public string? SubscriptionName { get; set; }

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

        public bool IsBeingConfigured { get; set; }
        public bool IsTestSubscription { get; set; }
        public bool IsFreeSubscription { get; set; }
        public bool UserIsTurnstileAdmin { get; set; }
        public bool UserIsSubscriberAdmin { get; set; }
        public bool IsSubscriptionUpdated { get; set; }
        public bool HasValidationErrors { get; set; }

        public List<SelectListItem> AvailableStates { get; set; } = new List<SelectListItem>();

        [Display(Name = "Management link(s)")]
        public Dictionary<string, string>? ManagementUrls { get; set; }

        [Display(Name = "Subscription created (UTC)")]
        public DateTime? CreatedDateTimeUtc { get; set; }

        [Display(Name = "Subscription last updated (UTC)")]
        public DateTime? StateLastUpdatedDateTimeUtc { get; set; }

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
