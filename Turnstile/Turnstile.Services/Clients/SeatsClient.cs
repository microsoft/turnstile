// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;

namespace Turnstile.Services.Clients;

public class SeatsClient : ISeatsClient
{
    private readonly HttpClient httpClient;
    private readonly ILogger logger;

    public SeatsClient(HttpClient httpClient, ILogger logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public async Task<SeatResult?> EnterTurnstile(SeatRequest seatRequest, string subscriptionId)
    {
        ArgumentNullException.ThrowIfNull(seatRequest, nameof(seatRequest));
        ArgumentNullException.ThrowIfNull(subscriptionId, nameof(subscriptionId));

        var url = $"api/saas/subscriptions/{subscriptionId}/entry";

        using (var apiRequest = new HttpRequestMessage(HttpMethod.Post, url))
        {
            apiRequest.Content = new StringContent(JsonConvert.SerializeObject(seatRequest));

            var apiResponse = await httpClient.SendAsync(apiRequest);

            if (apiResponse.IsSuccessStatusCode)
            {
                var jsonString = await apiResponse.Content.ReadAsStringAsync();
                var seatResult = JsonConvert.DeserializeObject<SeatResult>(jsonString);

                return seatResult!;
            }
            else
            {
                var apiError = await apiResponse.Content.ReadAsStringAsync();
                var errorMessage = $"Turnstile API POST [{url}] failed with status code [{apiResponse.StatusCode}]: [{apiError}]";

                logger.LogError(errorMessage);

                throw new HttpRequestException(errorMessage);
            }
        }
    }

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
            else if (apiResponse.IsSuccessStatusCode)
            {
                var jsonString = await apiResponse.Content.ReadAsStringAsync();
                var seat = JsonConvert.DeserializeObject<Seat>(jsonString);

                return seat;
            }
            else
            {
                var apiError = await apiResponse.Content.ReadAsStringAsync();
                var errorMessage = $"Turnstile API GET [{url}] failed with status code [{apiResponse.StatusCode}]: [{apiError}]";

                logger.LogError(errorMessage);

                throw new HttpRequestException(errorMessage);
            }
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

            if (!apiResponse.IsSuccessStatusCode)
            {
                var apiError = await apiResponse.Content.ReadAsStringAsync();
                var errorMessage = $"Turnstile API GET [{url}] failed with status code [{apiResponse.StatusCode}]: [{apiError}]";

                logger.LogError(errorMessage);

                throw new HttpRequestException(errorMessage);
            }

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

            if (!apiResponse.IsSuccessStatusCode)
            {
                var apiError = await apiResponse.Content.ReadAsStringAsync();
                var errorMessage = $"Turnstile API GET [{url}] failed with status code [{apiResponse.StatusCode}]: [{apiError}]";

                logger.LogError(errorMessage);

                throw new HttpRequestException(errorMessage);
            }

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

            if (!apiResponse.IsSuccessStatusCode)
            {
                var apiError = await apiResponse.Content.ReadAsStringAsync();
                var errorMessage = $"Turnstile API GET [{url}] failed with status code [{apiResponse.StatusCode}]: [{apiError}]";

                logger.LogError(errorMessage);

                throw new HttpRequestException(errorMessage);
            }

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
            else if (apiResponse.IsSuccessStatusCode)
            {
                var jsonString = await apiResponse.Content.ReadAsStringAsync();
                var seat = JsonConvert.DeserializeObject<Seat>(jsonString);

                return seat!;
            }
            else
            {
                var apiError = await apiResponse.Content.ReadAsStringAsync();
                var errorMessage = $"Turnstile API POST [{url}] failed with status code [{apiResponse.StatusCode}]: [{apiError}]";

                logger.LogError(errorMessage);

                throw new HttpRequestException(errorMessage);
            }
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

            if (!apiResponse.IsSuccessStatusCode)
            {
                var apiError = await apiResponse.Content.ReadAsStringAsync();
                var errorMessage = $"Turnstile API DELETE [{url}] failed with status code [{apiResponse.StatusCode}]: [{apiError}]";

                logger.LogError(errorMessage);

                throw new HttpRequestException(errorMessage);
            }
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
            else if (apiResponse.IsSuccessStatusCode)
            {
                var jsonString = await apiResponse.Content.ReadAsStringAsync();
                var seat = JsonConvert.DeserializeObject<Seat>(jsonString);

                return seat!;
            }
            else
            {
                var apiError = await apiResponse.Content.ReadAsStringAsync();
                var errorMessage = $"Turnstile API POST [{url}] failed with status code [{apiResponse.StatusCode}]: [{apiError}]";

                logger.LogError(errorMessage);

                throw new HttpRequestException(errorMessage);
            }
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
            else if (apiResponse.IsSuccessStatusCode)
            {
                var jsonString = await apiResponse.Content.ReadAsStringAsync();
                var seat = JsonConvert.DeserializeObject<Seat>(jsonString);

                return seat!;
            }
            else
            {
                var apiError = await apiResponse.Content.ReadAsStringAsync();
                var errorMessage = $"Turnstile API POST [{url}] failed with status code [{apiResponse.StatusCode}]: [{apiError}]";

                logger.LogError(errorMessage);

                throw new HttpRequestException(errorMessage);
            }
        }
    }
}
