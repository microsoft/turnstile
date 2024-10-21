// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Turnstile.Web.Common.Extensions
{
    public static class ControllerExtensions
    {
        public static IActionResult ServiceUnavailable(this Controller _) =>
            new StatusCodeResult((int)HttpStatusCode.ServiceUnavailable);
    }
}
