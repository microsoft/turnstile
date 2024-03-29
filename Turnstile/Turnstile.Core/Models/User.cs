﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using Turnstile.Core.Constants;

namespace Turnstile.Core.Models
{
    public class User
    {
        [JsonPropertyName("user_id")]
        [JsonProperty("user_id")]
        [OpenApiProperty(Nullable = false, Description = "This user's unique identifier")]
        public string? UserId { get; set; }

        [JsonPropertyName("user_name")]
        [JsonProperty("user_name")]
        [OpenApiProperty(Nullable = true, Description = "This user's display name")]
        public string? UserName { get; set; }

        [JsonPropertyName("tenant_id")]
        [JsonProperty("tenant_id")]
        [OpenApiProperty(Nullable = false, Description = "This user's tenant ID")]
        public string? TenantId { get; set; }

        [JsonPropertyName("email")]
        [JsonProperty("email")]
        [OpenApiProperty(Nullable = true, Description = "This user's primary email address")]
        public string? Email { get; set; }

        public IEnumerable<string> Validate()
        {
            if (string.IsNullOrEmpty(UserId))
            {
                yield return "User [user_id] is required.";
            }

            if (string.IsNullOrEmpty(TenantId))
            {
                yield return "User [tenant_id] is required.";
            }

            if (string.IsNullOrEmpty(Email))
            {
                yield return "User [email] is required.";
            }
        }

        public IEnumerable<string> ValidateSeatRequest(Subscription inSubscription)
        {
            ArgumentNullException.ThrowIfNull(inSubscription, nameof(inSubscription));

            var validationErrors = new List<string>(Validate());

            if (inSubscription.State != SubscriptionStates.Active)
            {
                validationErrors.Add(
                    $"Subscription [{inSubscription.SubscriptionId}] is currently [{inSubscription.State}]; " +
                    $"seats can be requested only in [{SubscriptionStates.Active}] subscriptions.");
            }

            return validationErrors;
        }
    }
}
