using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Turnstile.Core.Constants;

namespace Turnstile.Web.Models
{
    public class SeatingConfigurationViewModel
    {
        [Display(Name = "Seating strategy")]
        [Required(ErrorMessage = "Seating strategy is required.")]
        public string? SeatingStrategyName { get; set; } = SeatingStrategies.FirstComeFirstServed;

        public List<SelectListItem> AvailableSeatingStrategies { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Text = "First come, first served", Value = SeatingStrategies.FirstComeFirstServed, Selected = true },
            new SelectListItem { Text = "Monthly active user", Value = SeatingStrategies.MonthlyActiveUser }
        };

        [Display(Name = "Low seat warning level (% available seats)")]
        [Range(0, 100, ErrorMessage = "Low seat warning level must be between 0% and 100%.")] // We'll convert this to a percentage before we persist it...
        public double? LowSeatWarningLevelPercent { get; set; } = 25;

        [Display(Name = "Enable limited overflow seating?")]
        public bool? LimitedOverflowSeatingEnabled { get; set; }

        [Display(Name = "How many days should a seat remain active by default?")]
        [Range(1, 365, ErrorMessage = "A seat can be active between 1 and 365 days.")]
        [Required(ErrorMessage = "Seat expiration period is required.")]
        public int? SeatExpiryInDays { get; set; } = 7;

        [Display(Name = "How many days should a seat reservation remain active by default?")]
        [Range(1, 365, ErrorMessage = "A seat reservation can be active between 1 and 365 days.")]
        [Required(ErrorMessage = "Seat reservation expiration period is required.")]
        public int? SeatReservationExpiryInDays { get; set; }
    }
}
