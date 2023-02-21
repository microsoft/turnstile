// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Turnstile.Core.Models
{
    public class SeatingSummary
    {
        [JsonPropertyName("standard_seat_count")]
        [JsonProperty("standard_seat_count")]
        [OpenApiProperty(Nullable = false, Description = "Total number of occupied or reserved [standard] seats in this subscription")]
        public int StandardSeatCount { get; set; } = 0;

        [JsonPropertyName("limited_seat_count")]
        [JsonProperty("limited_seat_count")]
        [OpenApiProperty(Nullable = false, Description = "Total number of occupied [limited] seats in this subscription")]
        public int LimitedSeatCount { get; set; } = 0;
    }
}
