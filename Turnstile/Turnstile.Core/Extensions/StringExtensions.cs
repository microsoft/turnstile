﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace Turnstile.Core.Extensions
{
    public static class StringExtensions
    {
        public static string ToParagraph(this IEnumerable<string> inputStrings) =>
           string.Join(' ', inputStrings);

        public static string ToAndList(this IEnumerable<string> inputStrings) =>
            string.Join(" and ", inputStrings.Select(s => $"[{s}]"));

        public static string ToOrList(this IEnumerable<string> inputStrings) =>
            string.Join(" or ", inputStrings.Select(s => $"[{s}]"));

        public static bool IsValidEmailAddress(this string toTest) => 
            new EmailAddressAttribute().IsValid(toTest);
    }
}
