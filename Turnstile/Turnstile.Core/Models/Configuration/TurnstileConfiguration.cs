// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Turnstile.Core.Models.Configuration;

public class TurnstileConfiguration
{
    [JsonProperty("on_access_denied_url")]
    [JsonPropertyName("on_access_denied_url")]
    [OpenApiProperty(Nullable = true, Description = "If provided, the URL that a user will be redirected to when their access to the subscription has been denied (seat result code [access_denied])")]
    public string? OnAccessDeniedUrl { get; set; }

    [JsonProperty("on_access_granted_url")]
    [JsonPropertyName("on_access_granted_url")]
    [OpenApiProperty(Nullable = false, Description = "The URL that a user will be redirected to when they are provided a seat in the subscription (seat result code [seat_provided])")]
    public string? OnAccessGrantedUrl { get; set; }

    [JsonProperty("on_no_seat_available_url")]
    [JsonPropertyName("on_no_seat_available_url")]
    [OpenApiProperty(Nullable = true, Description = "If provided, the URL that a user will be redirected to when there are no more seats available in the subscription (seat result code [no_seats_available])")]
    public string? OnNoSeatAvailableUrl { get; set; }

    [JsonProperty("on_subscription_not_ready_url")]
    [JsonPropertyName("on_subscription_not_ready_url")]
    [OpenApiProperty(Nullable = true, Description = "If provided, the URL that a user will be redirected to when the subscription is not ready (seat result code [subscription_not_ready])")]
    public string? OnSubscriptionNotReadyUrl { get; set; }

    [JsonProperty("on_subscription_canceled_url")]
    [JsonPropertyName("on_subscription_canceled_url")]
    [OpenApiProperty(Nullable = true, Description = "If provided, the URL that a user will be redirected to when the subscription has been canceled (seat result code [subscription_canceled])")]
    public string? OnSubscriptionCanceledUrl { get; set; }

    [JsonProperty("on_subscription_suspended_url")]
    [JsonPropertyName("on_subscription_suspended_url")]
    [OpenApiProperty(Nullable = true, Description = "If provided, the URL that a user will be redirected to when the subscription has been suspended (seat result code [subscription_suspended])")]
    public string? OnSubscriptionSuspendedUrl { get; set; }

    [JsonProperty("on_subscription_not_found_url")]
    [JsonPropertyName("on_subscription_not_found_url")]
    [OpenApiProperty(Nullable = true, Description = "If provided, the URL that a user will be redirected to when the subscription can't be found (seat result code [subscription_not_found])")]
    public string? OnSubscriptionNotFoundUrl { get; set; }

    [JsonProperty("on_no_subscriptions_found_url")]
    [JsonPropertyName("on_no_subscriptions_found_url")]
    [OpenApiProperty(Nullable = true, Description = "If provided, the URL that a user will be redirected to when they don't have access to any subscriptions")]
    public string? OnNoSubscriptionsFoundUrl { get; set; }

    public IEnumerable<string> Validate()
    {
        if (string.IsNullOrEmpty(OnAccessGrantedUrl))
        {
            yield return "[on_access_granted_url] is required.";
        }
    }
}
