using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Turnstile.Api.Interfaces;
using Turnstile.Core.Constants;
using Turnstile.Core.Extensions;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Events.V_2022_03_18;
using Turnstile.Services.Clients;
using Turnstile.Services.Extensions;

namespace Turnstile.Api.Seats
{
    public class EnterTurnstile
    {
        private readonly HttpClient httpClient;
        private readonly IApiAuthorizationService authService;
        private readonly ISubscriptionEventPublisher eventPublisher;
        private readonly ILogger log;

        public EnterTurnstile(
            IApiAuthorizationService authService,
            ISubscriptionEventPublisher eventPublisher,
            ILogger<EnterTurnstile> log)
        {
            this.authService = authService;
            this.eventPublisher = eventPublisher;
            this.log = log;

            httpClient = new HttpClient().ConfigureForTurnstileApiAccess();
        }

        [Function("EnterTurnstile")]
        public async Task<IActionResult> RunEnterTurnstile(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "saas/subscriptions/{subscriptionId}/entry")] HttpRequest req,
            string subscriptionId)
        {
            try
            {
                if (await authService.IsAuthorized(req))
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

                    log.LogInformation(
                        $"Processing seat request [{seatRequest.RequestId}] in subscription [{subscriptionId}] for user [{seatRequest.UserId}].");

                    var subscriptionsClient = new SubscriptionsClient(httpClient, log);
                    var subscription = await subscriptionsClient.GetSubscription(subscriptionId);

                    if (subscription == null)                                       // Could we find the subscription?
                    {
                        log.LogInformation(
                            $"Unable to fulfill seat request [{seatRequest.RequestId}] in subscription [{subscriptionId}] " +
                            $"for user [{seatRequest.UserId}]. Subscription not found.");

                        return new OkObjectResult(new SeatResult(SeatResultCodes.SubscriptionNotFound, seatRequest));
                    }
                    else if (!HasAccessToSubscription(seatRequest, subscription))    // Does the user have access to the subscription?
                    {
                        log.LogInformation(
                            $"Unable to fulfill seat request [{seatRequest.RequestId}] in subscription [{subscriptionId}] " +
                            $"for user [{seatRequest.UserId}]. User does not have access to this subscription.");

                        await eventPublisher.PublishEvent(
                            new AdmissionDenied(subscription, seatRequest, SeatResultCodes.AccessDenied));

                        return new OkObjectResult(new SeatResult(SeatResultCodes.AccessDenied, seatRequest, subscription));
                    }
                    else if (subscription.State == SubscriptionStates.Purchased ||  // Is the subscription being configured?
                             subscription.IsBeingConfigured == true)
                    {
                        log.LogInformation(
                            $"Unable to fulfill seat request [{seatRequest.RequestId}] in subscription [{subscriptionId}] " +
                            $"for user [{seatRequest.UserId}]. Subscription is not ready.");

                        await eventPublisher.PublishEvent(
                            new AdmissionDenied(subscription, seatRequest, SeatResultCodes.SubscriptionNotReady));

                        return new OkObjectResult(new SeatResult(SeatResultCodes.SubscriptionNotReady, seatRequest, subscription));
                    }
                    else if (subscription.State == SubscriptionStates.Suspended)    // Is the subscription suspended?
                    {
                        log.LogInformation(
                           $"Unable to fulfill seat request [{seatRequest.RequestId}] in subscription [{subscriptionId}] " +
                           $"for user [{seatRequest.UserId}]. Subscription is [{subscription.State}].");

                        await eventPublisher.PublishEvent(
                            new AdmissionDenied(subscription, seatRequest, SeatResultCodes.SubscriptionSuspended));

                        return new OkObjectResult(new SeatResult(SeatResultCodes.SubscriptionSuspended, seatRequest, subscription));
                    }
                    else if (subscription.State == SubscriptionStates.Canceled)     // Is the subscription canceled?
                    {
                        log.LogInformation(
                           $"Unable to fulfill seat request [{seatRequest.RequestId}] in subscription [{subscriptionId}] " +
                           $"for user [{seatRequest.UserId}]. Subscription is [{subscription.State}].");

                        await eventPublisher.PublishEvent(
                            new AdmissionDenied(subscription, seatRequest, SeatResultCodes.SubscriptionCanceled));

                        return new OkObjectResult(new SeatResult(SeatResultCodes.SubscriptionCanceled, seatRequest, subscription));
                    }
                    else
                    {
                        // Alright, subscription looks good and we've confirmed that they have access to it.
                        // Let's try and get them a seat...

                        var seatResult = await TryGetSeat(subscription, seatRequest, log);

                        if (seatResult.ResultCode == SeatResultCodes.SeatProvided)
                        {
                            await eventPublisher.PublishEvent(
                                new AdmissionGranted(subscription, seatResult.Seat, seatRequest));
                        }
                        else
                        {
                            await eventPublisher.PublishEvent(
                                new AdmissionDenied(subscription, seatRequest, seatResult.ResultCode));
                        }

                        return new OkObjectResult(seatResult);
                    }
                }
                else
                {
                    return new ForbidResult();
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"An error occurred while trying to obtain a seat in subscription [{subscriptionId}]: [{ex.Message}].");

                throw;
            }
        }

        private async Task<SeatResult> TryGetSeat(Subscription subscription, SeatRequest seatRequest, ILogger log)
        {
            var seatsClient = new SeatsClient(httpClient, log);

            var user = new User
            {
                UserId = seatRequest.UserId,
                Email = seatRequest.EmailAddresses.First(),
                TenantId = seatRequest.TenantId,
                UserName = seatRequest.UserName ?? seatRequest.EmailAddresses.First()
            };

            var seat = await seatsClient.GetSeatByUserId(subscription.SubscriptionId!, user.UserId!);

            if (seat?.Occupant?.UserId == user.UserId)                                  // Does the user already have a seat?
            {
                log.LogInformation(
                    $"Subscription [{subscription.SubscriptionId}] seat request [{seatRequest.RequestId}] fulfilled for " +
                    $"user [{user.UserId}]. User already has a seat [{seat.SeatId}] in this subscription.");

                return new SeatResult(SeatResultCodes.SeatProvided, seatRequest, subscription, seat);
            }
            else if (seat?.Reservation?.UserId == user.UserId &&                        // Is a seat reserved for this user?
                     seat?.Reservation?.TenantId == user.TenantId)
            {
                log.LogInformation(
                   $"Subscription [{subscription.SubscriptionId}] seat request [{seatRequest.RequestId}] fulfilled for " +
                   $"user [{user.UserId}]. A seat [{seat.SeatId}] was reserved for this user.");

                seat = await seatsClient.RedeemSeat(subscription.SubscriptionId!, user, seat!.SeatId!) ??
                       throw new Exception($"Unable to redeem seat [{seat!.SeatId}] reserved for user [{user.TenantId}/{user.UserId}].");

                return new SeatResult(SeatResultCodes.SeatProvided, seatRequest, subscription, seat);
            }

            foreach (var email in seatRequest.EmailAddresses)                           // Is a seat reserved under any of this user's email addresses?
            {
                seat = await seatsClient.GetSeatByEmail(subscription.SubscriptionId!, email);

                if (seat != null)
                {
                    // No personally-identifiable information (PII) in the logs!
                    // We obfuscate email addresses here like this [cawatson@microsoft.com] == [c******n@microsoft.com].

                    const string emailObfuscationPattern = @"(?<=[\w]{1})[\w-\._\+%]*(?=[\w]{1}@)";

                    var obfuscatedEmail = Regex.Replace(email, emailObfuscationPattern, m => new string('*', m.Length));

                    log.LogInformation(
                        $"Subscription [{subscription.SubscriptionId}] seat request [{seatRequest.RequestId}] fulfilled for " +
                        $"user [{user.UserId}]. A seat [{seat.SeatId}] was reserved for this user under their email address [{obfuscatedEmail}].");

                    user.Email = email;

                    seat = await seatsClient.RedeemSeat(subscription.SubscriptionId!, user, seat!.SeatId!) ??
                           throw new Exception($"Unable to redeem seat [{seat!.SeatId}] reserved for user [{user.TenantId}/{user.UserId}].");

                    return new SeatResult(SeatResultCodes.SeatProvided, seatRequest, subscription, seat);
                }
            }

            seat = await seatsClient.RequestSeat(subscription.SubscriptionId!, user);   // Try to create a dynamic seat for this user.

            if (seat == null)
            {
                log.LogInformation(
                    $"Unable to fulfill seat request [{seatRequest.RequestId}] in subscription [{subscription.SubscriptionId!}] " +
                    $"for user [{seatRequest.UserId}]. There are no more seats available in this subscription.");

                return new SeatResult(SeatResultCodes.NoSeatsAvailable, seatRequest, subscription);
            }
            else
            {
                log.LogInformation(
                    $"Subscription [{subscription.SubscriptionId}] seat request [{seatRequest.RequestId}] fulfilled for " +
                    $"user [{user.UserId}]. A dynamic seat was created for this user.");

                return new SeatResult(SeatResultCodes.SeatProvided, seatRequest, subscription, seat);
            }
        }

        private static bool HasAccessToSubscription(SeatRequest seatRequest, Subscription subscription) =>
            // Seat requested by a member of the subscription's tenant, and...
            seatRequest.TenantId == subscription.TenantId &&
            // either no required user role has been defined on the subscription, or...
            // the seat is being requested by a member of the subscription's user role.
            (string.IsNullOrEmpty(subscription.UserRoleName) ||
             seatRequest.Roles.Select(r => r.ToLower()).Contains(subscription.UserRoleName.ToLower()));
    }
}
