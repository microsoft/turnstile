using Turnstile.Core.Models;

namespace Turnstile.Core.Interfaces
{
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
    }
}
