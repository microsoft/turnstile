using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Turnstile.Core.Constants;
using Turnstile.Core.Models.Configuration;

namespace Turnstile.Web.Models.PublisherConfig
{
    public class SeatingConfigurationViewModel : BaseConfigurationViewModel
    {
        public SeatingConfigurationViewModel() { }

        public SeatingConfigurationViewModel(PublisherConfiguration publisherConfig)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));

            if (publisherConfig.SeatingConfiguration != null)
            {
                var seatingConfig = publisherConfig.SeatingConfiguration;

                SeatingStrategyName = seatingConfig.SeatingStrategyName!;
                LimitedOverflowSeatingEnabled = seatingConfig.LimitedOverflowSeatingEnabled.GetValueOrDefault();
            }
        }

        public SeatingConfiguration ToCoreModel() =>
            new SeatingConfiguration
            {
                SeatingStrategyName = this.SeatingStrategyName, // The [this] is unnecessary but for readability's sake...
                LimitedOverflowSeatingEnabled = this.LimitedOverflowSeatingEnabled
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
    }
}
