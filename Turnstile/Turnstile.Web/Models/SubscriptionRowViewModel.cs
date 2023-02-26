// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Turnstile.Core.Models;

namespace Turnstile.Web.Models;

public class SubscriptionRowViewModel
{
    public SubscriptionRowViewModel() { }

    public SubscriptionRowViewModel(Subscription subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));

        SubscriptionId = subscription.SubscriptionId;
        SubscriptionName = subscription.SubscriptionName;
        State = subscription.State;
        TenantId = subscription.TenantId;
        OfferId = subscription.OfferId;
        PlanId = subscription.PlanId;
        SeatingStrategyName = subscription.SeatingConfiguration?.SeatingStrategyName;
        IsBeingConfigured = subscription.IsBeingConfigured;
        TotalSeats = subscription.TotalSeats;
        CreatedDateTimeUtc = subscription.CreatedDateTimeUtc;
        StateLastUpdatedDateTimeUtc = subscription.StateLastUpdatedDateTimeUtc;
    }

    public string? SubscriptionId { get; set; }
    public string? SubscriptionName { get; set; }
    public string? State { get; set; }
    public string? TenantId { get; set; }
    public string? TenantName { get; set; }
    public string? OfferId { get; set; }
    public string? PlanId { get; set; }
    public string? SeatingStrategyName { get; set; }

    public bool? IsBeingConfigured { get; set; }

    public int? TotalSeats { get; set; }

    public DateTime? CreatedDateTimeUtc { get; set; }
    public DateTime? StateLastUpdatedDateTimeUtc { get; set; }
}
