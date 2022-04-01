using Newtonsoft.Json;
using System.Net;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;

namespace Turnstile.Services.Clients
{
    public class SeatsClient : ISeatsClient
    {
        private readonly HttpClient httpClient;

        public SeatsClient(IHttpClientFactory httpClientFactory) =>
             httpClient = httpClientFactory.CreateClient(HttpClientNames.TurnstileApi);

        public async Task<Seat?> GetSeat(string subscriptionId, string seatId)
        {
            ArgumentNullException.ThrowIfNull(subscriptionId, nameof(subscriptionId));
            ArgumentNullException.ThrowIfNull(seatId, nameof(seatId));

            var url = $"api/saas/subscriptions/{subscriptionId}/seats/{seatId}";

            using (var apiRequest = new HttpRequestMessage(HttpMethod.Get, url))
            {
                var apiResponse = await httpClient.SendAsync(apiRequest);

                if (apiResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    return null; // Seat not found...
                }

                apiResponse.EnsureSuccessStatusCode();

                var jsonString = await apiResponse.Content.ReadAsStringAsync();
                var seat = JsonConvert.DeserializeObject<Seat>(jsonString);

                return seat;
            }
        }

        public async Task<Seat?> GetSeatByEmail(string subscriptionId, string userEmail)
        {
            ArgumentNullException.ThrowIfNull(subscriptionId, nameof(subscriptionId));
            ArgumentNullException.ThrowIfNull(userEmail, nameof(userEmail));

            var url = $"api/saas/subscriptions/{subscriptionId}/seats?user_email={userEmail.ToLower()}";

            using (var apiRequest = new HttpRequestMessage(HttpMethod.Get, url))
            {
                var apiResponse = await httpClient.SendAsync(apiRequest);

                apiResponse.EnsureSuccessStatusCode();

                var jsonString = await apiResponse.Content.ReadAsStringAsync();
                var seats = JsonConvert.DeserializeObject<List<Seat>>(jsonString);

                // If there is one, there should be only one...

                return seats!.FirstOrDefault();
            }
        }

        public async Task<Seat?> GetSeatByUserId(string subscriptionId, string userId)
        {
            ArgumentNullException.ThrowIfNull(subscriptionId, nameof(subscriptionId));
            ArgumentNullException.ThrowIfNull(userId, nameof(userId));

            var url = $"api/saas/subscriptions/{subscriptionId}/seats?user_id={userId.ToLower()}";

            using (var apiRequest = new HttpRequestMessage(HttpMethod.Get, url))
            {
                var apiResponse = await httpClient.SendAsync(apiRequest);

                apiResponse.EnsureSuccessStatusCode();

                var jsonString = await apiResponse.Content.ReadAsStringAsync();
                var seats = JsonConvert.DeserializeObject<List<Seat>>(jsonString);

                // If there is one, there should be only one...

                return seats!.FirstOrDefault();
            }
        }

        public async Task<IEnumerable<Seat>> GetSeats(string subscriptionId)
        {
            ArgumentNullException.ThrowIfNull(subscriptionId, nameof(subscriptionId));

            var url = $"api/saas/subscriptions/{subscriptionId}/seats";

            using (var apiRequest = new HttpRequestMessage(HttpMethod.Get, url))
            {
                var apiResponse = await httpClient.SendAsync(apiRequest);

                apiResponse.EnsureSuccessStatusCode();

                var jsonString = await apiResponse.Content.ReadAsStringAsync();
                var seats = JsonConvert.DeserializeObject<List<Seat>>(jsonString);

                return seats!;
            }
        }

        public async Task<Seat?> RedeemSeat(string subscriptionId, User user, string seatId)
        {
            ArgumentNullException.ThrowIfNull(subscriptionId, nameof(subscriptionId));
            ArgumentNullException.ThrowIfNull(user, nameof(user));
            ArgumentNullException.ThrowIfNull(seatId, nameof(seatId));

            var url = $"api/saas/subscriptions/{subscriptionId}/seats/{seatId}/redeem";

            using (var apiRequest = new HttpRequestMessage(HttpMethod.Post, url))
            {
                apiRequest.Content = new StringContent(JsonConvert.SerializeObject(user));

                var apiResponse = await httpClient.SendAsync(apiRequest);

                if (apiResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    return null; // Seat not found...
                }

                apiResponse.EnsureSuccessStatusCode();

                var jsonString = await apiResponse.Content.ReadAsStringAsync();
                var seat = JsonConvert.DeserializeObject<Seat>(jsonString);

                return seat!;
            }
        }

        public async Task ReleaseSeat(string subscriptionId, string seatId)
        {
            ArgumentNullException.ThrowIfNull(subscriptionId, nameof(subscriptionId));
            ArgumentNullException.ThrowIfNull(seatId, nameof(seatId));

            var url = $"api/saas/subscriptions/{subscriptionId}/seats/{seatId}";

            using (var apiRequest = new HttpRequestMessage(HttpMethod.Delete, url))
            {
                var apiResponse = await httpClient.SendAsync(apiRequest);

                apiResponse.EnsureSuccessStatusCode();
            }
        }

        public async Task<Seat?> RequestSeat(string subscriptionId, User user, string? seatId = null)
        {
            ArgumentNullException.ThrowIfNull(subscriptionId, nameof(subscriptionId));
            ArgumentNullException.ThrowIfNull(user, nameof(user));

            seatId ??= Guid.NewGuid().ToString();

            var url = $"api/saas/subscriptions/{subscriptionId}/seats/{seatId}/request";

            using (var apiRequest = new HttpRequestMessage(HttpMethod.Post, url))
            {
                apiRequest.Content = new StringContent(JsonConvert.SerializeObject(user));

                var apiResponse = await httpClient.SendAsync(apiRequest);

                if (apiResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    return null; // Seat not found...
                }

                apiResponse.EnsureSuccessStatusCode();

                var jsonString = await apiResponse.Content.ReadAsStringAsync();
                var seat = JsonConvert.DeserializeObject<Seat>(jsonString);

                return seat!;
            }
        }

        public async Task<Seat?> ReserveSeat(string subscriptionId, Reservation reservation, string? seatId = null)
        {
            ArgumentNullException.ThrowIfNull(subscriptionId, nameof(subscriptionId));
            ArgumentNullException.ThrowIfNull(reservation, nameof(reservation));

            seatId ??= Guid.NewGuid().ToString();

            var url = $"api/saas/subscriptions/{subscriptionId}/seats/{seatId}/reserve";

            using (var apiRequest = new HttpRequestMessage(HttpMethod.Post, url))
            {
                apiRequest.Content = new StringContent(JsonConvert.SerializeObject(reservation));

                var apiResponse = await httpClient.SendAsync(apiRequest);

                if (apiResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    return null; // Seat not found...
                }

                apiResponse.EnsureSuccessStatusCode();

                var jsonString = await apiResponse.Content.ReadAsStringAsync();
                var seat = JsonConvert.DeserializeObject<Seat>(jsonString);

                return seat!;
            }
        }
    }
}
