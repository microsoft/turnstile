using Turnstile.Core.Models;

namespace Turnstile.Core.Interfaces
{
    public interface ISeatResultCache
    {
        Task<string> CacheSeatResult(SeatResult seatResult);
    }
}
