// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using Turnstile.Core.Models.Configuration;
using Turnstile.Web.Models;

namespace Turnstile.Web.Extensions
{
    public static class ControllerExtensions
    {
        public static IActionResult ServiceUnavailable(this Controller controller) =>
            new StatusCodeResult((int)HttpStatusCode.ServiceUnavailable);
    }
}
