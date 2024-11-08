﻿using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Turnstile.Core.Models
{
    public class SeatResult
    {
        public SeatResult() { }

        public SeatResult(string resultCode, SeatRequest seatRequest, Subscription? subscription = null, Seat? seat = null)
        {
            RequestId = seatRequest.RequestId;
            ResultCode = resultCode;
            Seat = seat;
            Subscription = subscription;
        }

        [JsonProperty("request_id")]
        [JsonPropertyName("request_id")]
        [OpenApiProperty(Nullable = false, Description = "The original seat request ID.")]
        public string? RequestId { get; set; }

        [JsonProperty("result_code")]
        [JsonPropertyName("result_code")]
        [OpenApiProperty(Nullable = false, Description = "A human-readable code (inc doc URL here) indicating the result of the seating request")]
        public string? ResultCode { get; set; }

        [JsonProperty("seat")]
        [JsonPropertyName("seat")]
        [OpenApiProperty(Nullable = true, Description = "If applicable, the seat provided as a result of the seating request")]
        public Seat? Seat { get; set; }

        [JsonProperty("subscription")]
        [JsonPropertyName("subscription")]
        [OpenApiProperty(Nullable = true, Description = "If applicable, the subscription in which the seat was requested")]
        public Subscription? Subscription { get; set; }
    }
}
