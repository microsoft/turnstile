// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Turnstile.Core.Constants;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Configuration;

namespace Turnstile.Web.Models;

public class SubscriptionConfigurationViewModel
{
    public SubscriptionConfigurationViewModel() { }

    public SubscriptionConfigurationViewModel(
        PublisherConfiguration publisherConfig,
        Subscription subscription,
        ClaimsPrincipal forPrincipal)
    {
        ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));
        ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));
        ArgumentNullException.ThrowIfNull(forPrincipal, nameof(forPrincipal));

        SubscriptionName = subscription.SubscriptionName;

        SeatingStrategyName = 
            subscription.SeatingConfiguration?.SeatingStrategyName ?? 
            publisherConfig.SeatingConfiguration!.SeatingStrategyName;

        SeatExpiryInDays =
            subscription.SeatingConfiguration?.DefaultSeatExpiryInDays ??
            publisherConfig.SeatingConfiguration!.DefaultSeatExpiryInDays;

        SeatReservationExpiryInDays =
            subscription.SeatingConfiguration?.SeatReservationExpiryInDays ??
            publisherConfig.SeatingConfiguration!.SeatReservationExpiryInDays;
    } 

    public Subscription ApplyTo(Subscription patch)
    {
        ArgumentNullException.ThrowIfNull(patch, nameof(patch));

        patch.SubscriptionName = SubscriptionName;

        patch.SeatingConfiguration = new SeatingConfiguration
        {
            SeatingStrategyName = SeatingStrategyName,
            DefaultSeatExpiryInDays = SeatExpiryInDays,
            SeatReservationExpiryInDays = SeatReservationExpiryInDays
        };

        return patch;
    }

    [Display(Name = "Subscription name")]
    [Required(ErrorMessage = "Subscription name is required.")]
    public string? SubscriptionName { get; set; }

    [Display(Name = "Seating strategy")]
    public string? SeatingStrategyName { get; set; }

    public List<SelectListItem> AvailableSeatingStrategies { get; set; } = new List<SelectListItem>
    {
        new SelectListItem { Text = "First come, first served", Value = SeatingStrategies.FirstComeFirstServed, Selected = true },
        new SelectListItem { Text = "Monthly active user", Value = SeatingStrategies.MonthlyActiveUser }
    };

    [Display(Name = "Default seat expiry in days")]
    [Range(1, 365, ErrorMessage = "Default seat expiry must be between 1 and 365 days.")]
    [Required(ErrorMessage = "Default seat expiry is required.")]
    public int? SeatExpiryInDays { get; set; } = 7;

    [Display(Name = "Seat reservation expiry in days")]
    [Range(1, 365, ErrorMessage = "Seat reservation expiry must be between 1 and 365 days.")]
    [Required(ErrorMessage = "Seat reservation expiry is required.")]
    public int? SeatReservationExpiryInDays { get; set; }
}
