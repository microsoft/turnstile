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
            LowSeatWarningEnabled = (seatingConfig.LowSeatWarningLevelPercent != null);
            LimitedOverflowSeatingEnabled = seatingConfig.LimitedOverflowSeatingEnabled.GetValueOrDefault();
            SeatExpiryInDays = seatingConfig.DefaultSeatExpiryInDays;
            SeatReservationExpiryInDays = seatingConfig.SeatReservationExpiryInDays;
        }

        public SeatingConfiguration ToCoreModel() =>
            new SeatingConfiguration
            {
                SeatingStrategyName = this.SeatingStrategyName, // The [this] is unnecessary but for readability's sake...
                LowSeatWarningLevelPercent = (LowSeatWarningEnabled == true ? SeatingConfiguration.DefaultLowSeatWarningLevelPercent : default(double?)),
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

        // This is a little funky. Originally, I had planned on letting customers
        // set the "low seating warning level pct" through the UI but, the more I thought about it,
        // the more I think it was too complicated. For now, we just give them a toggle that determines
        // whether or not they want to publish the events. If they want to publish the event, we set
        // the level to a default defined at [SeatingConfiguration.DefaultLowSeatWarningLevel] (as of this comment, 0.25/25%).
        // If an ISV really wants to, they can manually set this value using the API but we'll see if they actually need it or not.

        [Display(Name = "Publish low seat warning level events")]
        public bool LowSeatWarningEnabled { get; set; }

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
