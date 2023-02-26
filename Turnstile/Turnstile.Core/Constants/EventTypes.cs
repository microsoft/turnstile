// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Turnstile.Core.Constants;

public static class EventTypes
{
    public const string SeatRedeemed = "seat_redeemed";
    public const string SeatProvided = "seat_provided";
    public const string SeatReleased = "seat_released";
    public const string SeatReserved = "seat_reserved";
    public const string NoSeatsAvailable = "no_seats_available";
    public const string SeatingWarningLeavelReached = "seat_warning_level_reached";
    public const string SubscriptionUpdated = "subscription_updated";
    public const string SubscriptionCreated = "subscription_created";
}
