﻿using Turnstile.Core.Constants;
using Turnstile.Core.Models;

namespace Turnstile.Web.Models
{
    public class SeatViewModel
    {
        public SeatViewModel() { }

        public SeatViewModel(Seat seat)
        {
            ArgumentNullException.ThrowIfNull(seat, nameof(seat));

            SeatId = seat.SeatId;

            if (seat.Occupant != null)
            {
                IsLimited = seat.SeatType == SeatTypes.Limited;
                UserId = seat.Occupant!.UserId;
                UserEmail = seat.Occupant!.Email;
                UserName = seat.Occupant!.UserName;
            }
            else
            {
                IsReserved = true;
                UserId = seat.Reservation!.UserId;
                UserEmail = seat.Reservation!.Email;
                UserName = seat.Reservation!.Email;
            }
        }

        public string? SeatId { get; set; }

        public string? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? UserName { get; set; }

        public bool IsLimited { get; set; } = false;
        public bool IsReserved { get; set; } = false;
    }
}
