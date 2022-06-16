// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Turnstile.Core.Constants;
using Turnstile.Core.Extensions;
using Turnstile.Core.Models;
using Turnstile.Services.Clients;
using static System.Environment;
using static Turnstile.Core.Constants.EnvironmentVariableNames;

namespace Turnstile.Api.Seats
{
    public static class Turnstile
    {
        private static readonly HttpClient httpClient;

        static Turnstile()
        {
            httpClient = new HttpClient();

            httpClient.BaseAddress = new Uri(
                GetEnvironmentVariable(ApiAccess.BaseUrl) ??
                throw new InvalidOperationException($"[{ApiAccess.BaseUrl}] environment variable not configured"));

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Turnstile.Api");

            httpClient.DefaultRequestHeaders.Add("x-functions-key",
                GetEnvironmentVariable(ApiAccess.AccessKey ??
                throw new InvalidOperationException($"[{ApiAccess.AccessKey}] environment variable not configured.")));
        }

        [FunctionName("Turnstile")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "saas/subscriptions/{subscriptionId}/turnstile")] HttpRequest req,
            ILogger log, string subscriptionId)
        {
            try
            {
                var httpContent = await new StreamReader(req.Body).ReadToEndAsync();

                if (string.IsNullOrEmpty(httpContent))
                {
                    return new BadRequestObjectResult("User is required.");
                }

                var seatRequest = JsonConvert.DeserializeObject<SeatRequest>(httpContent);

                if (string.IsNullOrEmpty(seatRequest.UserId) ||
                    string.IsNullOrEmpty(seatRequest.TenantId) ||
                    seatRequest.EmailAddresses.None())
                {
                    return new BadRequestObjectResult("[user_id], [tenant_id], and at least one [email(s)] are required.");
                }

                var subscriptionsClient = new SubscriptionsClient(httpClient, log);
                var subscription = await subscriptionsClient.GetSubscription(subscriptionId);

                if (subscription == null)                                       // Could we find the subscription?
                {
                    return new OkObjectResult(new SeatResult(SeatResultCodes.SubscriptionNotFound));
                }
                else if (!seatRequest.HasAccessToSubscription(subscription))    // Does the user have access to the subscription?
                {
                    return new OkObjectResult(new SeatResult(SeatResultCodes.AccessDenied, subscription));
                }
                else if (subscription.State == SubscriptionStates.Purchased ||  // Is the subscription being configured?
                         subscription.IsBeingConfigured == true)
                {
                    return new OkObjectResult(new SeatResult(SeatResultCodes.SubscriptionUnavailable, subscription));
                }
                else if (subscription.State == SubscriptionStates.Suspended)    // Is the subscription suspended?
                {
                    return new OkObjectResult(new SeatResult(SeatResultCodes.SubscriptionSuspended, subscription));
                }
                else if (subscription.State == SubscriptionStates.Canceled)     // Is the subscription canceled?
                {
                    return new OkObjectResult(new SeatResult(SeatResultCodes.SubscriptionCanceled, subscription));
                }
                else
                {
                    // Alright, subscription looks good and we've confirmed that they have access to it.
                    // Let's try and get them a seat...

                    return new OkObjectResult(await TryGetSeat(subscription, seatRequest, log));
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"An error occurred while trying to obtain a seat in subscription [{subscriptionId}]: [{ex.Message}].");

                throw;
            }
        }

        private static async Task<SeatResult> TryGetSeat(Subscription subscription, SeatRequest seatRequest, ILogger log)
        {
            var seatsClient = new SeatsClient(httpClient, log);

            var user = new User
            {
                UserId = seatRequest.UserId,
                Email = seatRequest.EmailAddresses.First(),
                TenantId = seatRequest.TenantId,
                UserName = seatRequest.EmailAddresses.First()
            };

            var seat = await seatsClient.GetSeatByUserId(subscription.SubscriptionId!, user.UserId!);

            if (seat?.Occupant?.UserId == user.UserId)                                  // Does the user already have a seat?
            {
                return new SeatResult(SeatResultCodes.SeatProvided, subscription, seat);
            }
            else if (seat?.Reservation?.UserId == user.UserId &&                        // Is a seat reserved for this user?
                     seat?.Reservation?.TenantId == user.TenantId) 
            {
                seat = await seatsClient.RedeemSeat(subscription.SubscriptionId!, user, seat!.SeatId!) ??
                       throw new Exception($"Unable to redeem seat [{seat!.SeatId}] reserved for user [{user.TenantId}/{user.UserId}].");

                return new SeatResult(SeatResultCodes.SeatProvided, subscription, seat);
            }

            foreach (var email in seatRequest.EmailAddresses)                           // Is a seat reserved under any of this user's email addresses?
            {
                seat = await seatsClient.GetSeatByEmail(subscription.SubscriptionId!, email);

                if (seat != null)
                {
                    user.Email = email;

                    seat = await seatsClient.RedeemSeat(subscription.SubscriptionId!, user, seat!.SeatId!) ??
                           throw new Exception($"Unable to redeem seat [{seat!.SeatId}] reserved for user [{user.TenantId}/{user.UserId}].");

                    return new SeatResult(SeatResultCodes.SeatProvided, subscription, seat);
                }
            }

            seat = await seatsClient.RequestSeat(subscription.SubscriptionId!, user);   // Try to create a dynamic seat for this user.

            if (seat == null)
            {
                return new SeatResult(SeatResultCodes.NoSeatsAvailable, subscription);
            }
            else
            {
                return new SeatResult(SeatResultCodes.SeatProvided, subscription, seat);
            }
        }

        private static bool HasAccessToSubscription(this SeatRequest seatRequest, Subscription subscription) =>
            // Seat requested by a member of the subscription's tenant, and...
            seatRequest.TenantId == subscription.TenantId &&
            // either no required user role has been defined on the subscription, or...
            // the seat is being requested by a member of the subscription's user role.
            (string.IsNullOrEmpty(subscription.UserRoleName) || 
             seatRequest.Roles.Select(r => r.ToLower()).Contains(subscription.UserRoleName.ToLower()));
    }
}
