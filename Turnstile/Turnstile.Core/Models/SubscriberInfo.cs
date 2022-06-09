// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Turnstile.Core.Models
{
    public class SubscriberInfo
    {
        [JsonProperty("tenant_country")]
        [JsonPropertyName("tenant_country")]
        public string? TenantCountry { get; set; }
    }
}
