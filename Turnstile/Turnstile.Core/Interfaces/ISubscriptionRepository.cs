using Turnstile.Core.Models;

namespace Turnstile.Core.Interfaces
{
    public interface ISubscriptionRepository
    {
        /// <summary>
        /// Creates a new subscription
        /// </summary>
        /// <param name="subscription">The new subscription</param>
        /// <returns>The new subscription</returns>
        Task<Subscription> CreateSubscription(Subscription subscription);

        /// <summary>
        /// Replaces an existing subscription
        /// </summary>
        /// <param name="subscription">The updated subscription</param>
        /// <returns>The updated subscription</returns>
        Task<Subscription> ReplaceSubscription(Subscription subscription);

        /// <summary>
        /// Gets subscriptions
        /// </summary>
        /// <param name="byState">by state</param>
        /// <param name="byOfferId">by offer ID</param>
        /// <param name="byPlanId">by plan ID</param>
        /// <param name="byTenantId">by tenant ID</param>
        /// <returns>Subscriptions that match query criteria</returns>
        Task<IList<Subscription>> GetSubscriptions(
            string? byState = null,
            string? byOfferId = null,
            string? byPlanId = null,
            string? byTenantId = null);

        /// <summary>
        /// Gets a subscription by its <paramref name="subscriptionId"/>
        /// </summary>
        /// <param name="subscriptionId">by subscription ID</param>
        /// <returns>The subscription if found, null if not</returns>
        Task<Subscription?> GetSubscription(string subscriptionId);

        /// <summary>
        /// Gets seats
        /// </summary>
        /// <param name="subscriptionId">by subscription ID</param>
        /// <param name="byUserId">by user ID</param>
        /// <param name="byUserEmail">by user email</param>
        /// <returns>Seats that match query criteria</returns>
        Task<IList<Seat>> GetSeats(string subscriptionId,
            string? byUserId = null,
            string? byUserEmail = null);

        /// <summary>
        /// Gets a seat by its [<paramref name="seatId"/>] and [<paramref name="subscriptionId"/>]
        /// </summary>
        /// <param name="seatId">The seat ID</param>
        /// <param name="subscriptionId">The subscription ID</param>
        /// <returns>The seat if found, null if not</returns>
        Task<Seat?> GetSeat(string seatId, string subscriptionId);

        /// <summary>
        /// Atempts to create a new seat in subscription [<paramref name="subscription"/>]
        /// </summary>
        /// <param name="seat">The seat to create</param>
        /// <param name="subscription">The subscription to create the seat in</param>
        /// <returns>The result of the seat creation operation</returns>
        Task<SeatCreationContext> CreateSeat(Seat seat, Subscription subscription);

        /// <summary>
        /// Replaces an existing seat
        /// </summary>
        /// <param name="seat">The updated seat</param>
        /// <returns>The updated seat</returns>
        Task<Seat> ReplaceSeat(Seat seat);

        /// <summary>
        /// Deletes seat [<paramref name="seatId"/>]
        /// </summary>
        /// <param name="seatId">The seat ID</param>
        /// <param name="subscriptionId">The subscription ID</param>
        /// <returns></returns>
        Task DeleteSeat(string seatId, string subscriptionId);
    }
}
