// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Turnstile.Core.Constants
{
    public static class SeatingStrategies
    {
        // Add your custom seating strategies here...

        public const string MonthlyActiveUser = "monthly_active_user";
        public const string FirstComeFirstServed = "first_come_first_served";

        // And here...

        public static readonly string[] ValidStrategies =
            new[] { MonthlyActiveUser, FirstComeFirstServed };
    }
}
