// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using Turnstile.Core.Constants;

namespace Turnstile.Core.Models
{
    public class Reservation
    {
        [JsonPropertyName("user_id")]
        [JsonProperty("user_id")]
        [OpenApiProperty(Nullable = true, Description = "User (ID) that this seat is reserved for; required if [email] not provided.")]
        public string? UserId { get; set; }

        [JsonPropertyName("tenant_id")]
        [JsonProperty("tenant_id")]
        [OpenApiProperty(Nullable = true, Description = "User (tenant ID) that this seat is reserved for; required if [email] not provided")]
        public string? TenantId { get; set; }

        [JsonPropertyName("email")]
        [JsonProperty("email")]
        [OpenApiProperty(Nullable = true, Description = "User (email address) that this seat is reserved for; required if [user_id] and/or [tenant_id] not provided")]
        public string? Email { get; set; }

        [JsonPropertyName("invite_url")]
        [JsonProperty("invite_url")]
        [OpenApiProperty(Nullable = true, Description = "URL for user to redeem seat reservation")]
        public string? InvitationUrl { get; set; }

        public IEnumerable<string> Validate(Subscription inSubscription)
        {
            ArgumentNullException.ThrowIfNull(inSubscription, nameof(inSubscription));

            if (string.IsNullOrEmpty(Email) && (string.IsNullOrEmpty(TenantId) || string.IsNullOrEmpty(UserId)))
            {
                yield return "Reservation ([user_id] and [tenant_id]) or [email] is required.";
            }

            if (inSubscription.State != SubscriptionStates.Active)
            {
                yield return
                    $"Subscription [{inSubscription.SubscriptionId}] is currently [{inSubscription.State}]; " +
                    $"seats can be reserved only in [{SubscriptionStates.Active}] subscriptions.";
            }
        }
    }
}
