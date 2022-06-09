// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Turnstile.Core.Constants;
using Turnstile.Core.Models;

namespace Turnstile.Web.Models
{
    public class OccupiedSeatViewModel
    {
        public OccupiedSeatViewModel() { }

        public OccupiedSeatViewModel(Seat seat)
        {
            ArgumentNullException.ThrowIfNull(seat, nameof(seat));

            SeatId = seat.SeatId;
            UserId = seat.Occupant!.UserId;
            UserName = seat.Occupant!.UserName;
            IsLimited = (seat.SeatType == SeatTypes.Limited);
            ExpiresDateTimeUtc = seat.ExpirationDateTimeUtc;
            ProvidedDateTimeUtc = seat.CreationDateTimeUtc;
        }

        public string? SeatId { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }

        public bool IsLimited { get; set; } = false;

        public DateTime? ExpiresDateTimeUtc { get; set; }
        public DateTime? ProvidedDateTimeUtc { get; set; }
    }
}
