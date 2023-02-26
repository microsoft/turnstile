// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Turnstile.Web.Models.Mona;

public class MarketplaceTerm
{
    [JsonProperty("termUnit")]
    [JsonPropertyName("termUnit")]
    public string? TermUnit { get; set; }

    [JsonProperty("startDate")]
    [JsonPropertyName("startDate")]
    public DateTime? StartDate { get; set; }

    [JsonProperty("endDate")]
    [JsonPropertyName("endDate")]
    public DateTime? EndDate { get; set; }
}
