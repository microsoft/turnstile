// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Turnstile.Core.Models.Events.V_2022_03_18
{
    public class BaseSeatEvent : BaseSubscriptionEvent
    {
        protected BaseSeatEvent(string eventType)
            : base(eventType) { }

        protected BaseSeatEvent(string eventType, Subscription subscription, Seat seat)
            : base(eventType, subscription)
        {
            ArgumentNullException.ThrowIfNull(seat, nameof(seat));

            Seat = seat;
        }

        [JsonPropertyName("seat")]
        public Seat? Seat { get; set; }

        public new class PropertiesViewModel : BaseSubscriptionEvent.PropertiesViewModel
        {
            public PropertiesViewModel() : base() { }

            public PropertiesViewModel(BaseSeatEvent @event) : base(@event)
            {
                SeatId = @event.Seat?.SeatId;
                OccupantUserId = @event.Seat?.Occupant?.UserId;
                OccupantName = @event.Seat?.Occupant?.UserName;
                OccupantEmail = @event.Seat?.Occupant?.Email;
                OccupantSeatingStrategyName = @event.Seat?.SeatingStrategyName;
                SeatType = @event.Seat?.SeatType;
                ReservationUserId = @event.Seat?.Reservation?.UserId;
                ReservationUserEmail = @event.Seat?.Reservation?.Email;
                SeatInvitationUrl = @event.Seat?.Reservation?.InvitationUrl;
            }

            [JsonPropertyName("Seat ID")]
            public string? SeatId { get; set; }

            [JsonPropertyName("Occupant ID")]
            public string? OccupantUserId { get; set; }

            [JsonPropertyName("Occupant Name")]
            public string? OccupantName { get; set; }

            [JsonPropertyName("Occupant Email")]
            public string? OccupantEmail { get; set; }

            [JsonPropertyName("Occupant Seating Strategy")]
            public string? OccupantSeatingStrategyName { get; set; }

            [JsonPropertyName("Seat Type")]
            public string? SeatType { get; set; }

            [JsonPropertyName("Reserved for User ID")]
            public string? ReservationUserId { get; set; }

            [JsonPropertyName("Reserved for User Email")]
            public string? ReservationUserEmail { get; set; }

            [JsonPropertyName("Seat Invitation URL")]
            public string? SeatInvitationUrl { get; set; }
        }
    }
}
