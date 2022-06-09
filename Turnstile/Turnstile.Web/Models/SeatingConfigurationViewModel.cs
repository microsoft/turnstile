// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Turnstile.Core.Constants;
using Turnstile.Core.Models.Configuration;

namespace Turnstile.Web.Models
{
    public class SeatingConfigurationViewModel
    {
        public SeatingConfigurationViewModel() { }

        public SeatingConfigurationViewModel(SeatingConfiguration seatingConfig)
        {
            ArgumentNullException.ThrowIfNull(seatingConfig, nameof(seatingConfig));

            SeatingStrategyName = seatingConfig.SeatingStrategyName!;
            LimitedOverflowSeatingEnabled = seatingConfig.LimitedOverflowSeatingEnabled.GetValueOrDefault();
            SeatExpiryInDays = seatingConfig.DefaultSeatExpiryInDays;
            SeatReservationExpiryInDays = seatingConfig.SeatReservationExpiryInDays;
        }

        public SeatingConfiguration ToCoreModel() =>
            new SeatingConfiguration
            {
                SeatingStrategyName = this.SeatingStrategyName, // The [this] is unnecessary but for readability's sake...
                LimitedOverflowSeatingEnabled = this.LimitedOverflowSeatingEnabled,
                DefaultSeatExpiryInDays = SeatExpiryInDays,
                SeatReservationExpiryInDays = SeatReservationExpiryInDays
            };

        [Display(Name = "Default seating strategy")]
        [Required(ErrorMessage = "Seating strategy is required.")]
        public string SeatingStrategyName { get; set; } = SeatingStrategies.FirstComeFirstServed;

        public List<SelectListItem> AvailableSeatingStrategies { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Text = "First come, first served", Value = SeatingStrategies.FirstComeFirstServed, Selected = true },
            new SelectListItem { Text = "Monthly active user", Value = SeatingStrategies.MonthlyActiveUser }
        };

        [Display(Name = "Provide limited seating when no other seats are available")]
        public bool LimitedOverflowSeatingEnabled { get; set; }

        [Display(Name = "Default seat expiry in days")]
        [Range(1, 365, ErrorMessage = "Default seat expiry must be between 1 and 365 days.")]
        [Required(ErrorMessage = "Default seat expiry is required.")]
        public int? SeatExpiryInDays { get; set; } = 7;

        [Display(Name = "Default seat reservation expiry in days")]
        [Range(1, 365, ErrorMessage = "Default seat reservation expiry must be between 1 and 365 days.")]
        [Required(ErrorMessage = "Default seat reservation expiry is required.")]
        public int? SeatReservationExpiryInDays { get; set; }
    }
}
