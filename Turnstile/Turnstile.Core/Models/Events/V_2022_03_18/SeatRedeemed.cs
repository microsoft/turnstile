// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Turnstile.Core.Constants;

namespace Turnstile.Core.Models.Events.V_2022_03_18;

public class SeatRedeemed : BaseSeatEvent
{
    public SeatRedeemed()
        : base(EventTypes.SeatRedeemed) { }

    public SeatRedeemed(Subscription subscription, Seat seat)
        : base(EventTypes.SeatRedeemed, subscription, seat) { }
}
