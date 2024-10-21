// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Turnstile.Core.Extensions
{
    public static class EnumerableExtensions
    {
        public static bool None<T>(this IEnumerable<T> inSet) =>
            inSet?.Any() == false;

        public static bool None<T>(this IEnumerable<T> inSet, Func<T, bool> predicate) =>
            inSet?.Any(predicate) == false;
    }
}
