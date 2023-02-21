// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Turnstile.Core.Models
{
    public class SubscriberInfo
    {
        [JsonProperty("tenant_country")]
        [JsonPropertyName("tenant_country")]
        [OpenApiProperty(Nullable = true, Description = "The subscriber's country")]
        public string? TenantCountry { get; set; }
    }
}
