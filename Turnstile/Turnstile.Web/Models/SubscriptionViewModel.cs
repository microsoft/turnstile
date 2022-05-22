using Turnstile.Core.Models;
using Turnstile.Core.Models.Configuration;
using System.Security.Claims;
using Turnstile.Core.Constants;

namespace Turnstile.Web.Models
{
    public class SubscriptionViewModel
    {
        public string? SubscriptionId { get; set; }
        public string? SubscriptionName { get; set; }
        public string? TenantId { get; set; }
        public string? TenantName { get; set; }
        public string? State { get; set; }
        public string? OfferId { get; set; }
        public string? PlanId { get; set; }
        public string? AdminRoleName { get; set; }
        public string? UserRoleName { get; set; }
        public string? AdminName { get; set; }
        public string? AdminEmail { get; set; }

        public bool IsBeingConfigured { get; set; }
        public bool IsTestSubscription { get; set; }
        public bool IsFreeSubscription { get; set; }
        public bool UserIsTurnstileAdmin { get; set; }
        public bool UserIsSubscriberAdmin { get; set; }

        public Dictionary<string, string>? ManagementUrls { get; set; }

        public DateTime? CreatedDateTimeUtc { get; set; }
        public DateTime? StateLastUpdatedDateTimeUtc { get; set; }

        public SeatsViewModel? Seating { get; set; }

        //from setup
        public SubscriberInfoViewModel? SubscriberInfo { get; set; }
        public SubscriptionConfigurationViewModel? SubscriptionConfiguration { get; set; }


        public SubscriptionViewModel() { }

        public SubscriptionViewModel(Subscription subscription, IEnumerable<Seat> seats,

                PublisherConfiguration publisherConfig, ClaimsPrincipal forPrincipal,

                bool userIsTurnstileAdmin = false, bool userIsSubscriberAdmin = false
                )
        {
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));
            ArgumentNullException.ThrowIfNull(seats, nameof(seats));

            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));
            ArgumentNullException.ThrowIfNull(forPrincipal, nameof(forPrincipal));

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

            Seating = new SeatsViewModel(subscription, seats);


            SubscriberInfo = new SubscriberInfoViewModel(subscription, forPrincipal);
            SubscriptionConfiguration = new SubscriptionConfigurationViewModel(publisherConfig, subscription, forPrincipal);
        }



        public string ChooseStateRowClass()
        {
            return State switch
            {
                SubscriptionStates.Active => "table-primary",
                SubscriptionStates.Canceled => "table-danger",
                SubscriptionStates.Suspended => "table-warning",
                SubscriptionStates.Purchased => "table-success",
                _ => string.Empty
            };
        }

        public string ChooseSeatingMeterClass()
        {
            if (Seating!.HasNoMoreSeats)
            {
                return "progress-bar progress-bar-striped bg-danger";
            }
            else if (Seating!.HasReachedLowSeatLevel)
            {
                return "progress-bar progress-bar-striped bg-warning";
            }
            else
            {
                return "progress-bar progress-bar-striped bg-success";
            }
        }

        public string DescribeState()
        {
            return State switch
            {
                SubscriptionStates.Active => "Active",
                SubscriptionStates.Canceled => "Canceled",
                SubscriptionStates.Suspended => "Suspended",
                SubscriptionStates.Purchased => "Provisioning",
                _ => throw new InvalidOperationException($"Subscription state [{State}] not supported.")
            };
        }

        public string DescribeSeatingStrategy()
        {
            return Seating!.SeatingStrategyName switch
            {
                SeatingStrategies.FirstComeFirstServed => "First come, first served",
                SeatingStrategies.MonthlyActiveUser => "Monthly active user",
                _ => throw new InvalidOperationException($"Subscription seating strategy [{Seating!.SeatingStrategyName}] not supported.")
            };
        }


        public string IsDisabledAttr()
        {
            return UserIsSubscriberAdmin ? " disabled " : "";
        }

        public bool IsDisabled()
        {
            return true;
        }
    }
}
