// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Turnstile.Core.Constants;
using Turnstile.Core.Models.Configuration;

namespace Turnstile.Api.Extensions
{
    public static class SeatingConfigurationExtensions
    {
        public static DateTime? CalculateSeatExpirationDate(this SeatingConfiguration seatConfiguration)
        {
            var now = DateTime.UtcNow;

            return seatConfiguration.SeatingStrategyName switch
            {
                SeatingStrategies.MonthlyActiveUser => new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month)).AddDays(1),
                SeatingStrategies.FirstComeFirstServed => now.Date.AddDays((double?)seatConfiguration.DefaultSeatExpiryInDays ?? 1),
                _ => throw new ArgumentException($"Seating strategy [{seatConfiguration.SeatingStrategyName}] not supported.")
            };
        }

        public static DateTime? CalculateSeatReservationExpirationDate(this SeatingConfiguration seatConfiguration) =>
            DateTime.UtcNow.AddDays((double)seatConfiguration.DefaultSeatExpiryInDays);
    }
}
