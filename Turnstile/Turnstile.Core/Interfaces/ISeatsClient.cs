// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Turnstile.Core.Models;

namespace Turnstile.Core.Interfaces;

public interface ISeatsClient
{
    Task<Seat?> GetSeat(string subscriptionId, string seatId);
    Task<Seat?> GetSeatByUserId(string subscriptionId, string userId);
    Task<Seat?> GetSeatByEmail(string subscriptionId, string userEmail);
    Task<Seat?> RedeemSeat(string subscriptionId, User user, string seatId);
    Task<Seat?> RequestSeat(string subscriptionId, User user, string? seatId = null);
    Task<Seat?> ReserveSeat(string subscriptionId, Reservation reservation, string? seatId = null);
    Task<IEnumerable<Seat>> GetSeats(string subscriptionId);
    Task ReleaseSeat(string subscriptionId, string seatId);

    // TODO: We might want to rethink how we call this Turnstile API later but, for now, we'll
    // just roll it into the seats client. After all, the [EnterTurnstile] API does roll up under
    // the seats folder in the function app.

    Task<SeatResult?> EnterTurnstile(SeatRequest seatRequest, string subscriptionId);
}
