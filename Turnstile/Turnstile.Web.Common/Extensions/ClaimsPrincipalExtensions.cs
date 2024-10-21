// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Identity.Web;
using System.Security.Claims;
using Turnstile.Core.Extensions;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Configuration;

namespace Turnstile.Web.Common.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public const string MsaTenantId = "9188040d-6c67-4c5b-b112-36a304b66dad";

        // A Microsoft Account (MSA) is just like any other organization (work/school) account except
        // it belongs to a specific tenant ID -- `MsaTenantId`. Right now, Turnstile doesn't support MSA
        // because Turnstile uses the tenant ID (unique if organizational AAD accounts) as part of its decision
        // whether or not to grant a user access to a SaaS subscription. We can't use the MSA tenant ID for this
        // decision because anyone can create a MSA and therefore have the `MsaTenantId` tenant ID. So, right now,
        // we just don't bother with it.

        public static bool IsMsa(this ClaimsPrincipal principal) =>
            principal.GetTenantId() == MsaTenantId;

        public static string? GetUserId(this ClaimsPrincipal principal, ClaimsConfiguration claimsConfig) =>
            claimsConfig.UserIdClaimTypes?
            .Select(principal.FindFirst)?
            .FirstOrDefault(claim => claim is not null)?.Value;

        public static string? GetUserName(this ClaimsPrincipal principal, ClaimsConfiguration claimsConfig) =>
            claimsConfig.UserNameClaimTypes?
            .Select(principal.FindFirst)?
            .FirstOrDefault(claim => claim is not null)?.Value;

        public static string? GetTenantId(this ClaimsPrincipal principal, ClaimsConfiguration claimsConfig) =>
            claimsConfig.TenantIdClaimTypes?
            .Select(principal.FindFirst)?
            .FirstOrDefault(claim => claim is not null)?.Value;

        public static string[] GetEmails(this ClaimsPrincipal principal, ClaimsConfiguration claimsConfig) =>
            claimsConfig.EmailClaimTypes?
            .Select(principal.FindAll)?
            .SelectMany(c => c.Select(claim => claim.Value.ToLower()))
            .Distinct()
            .Where(e => e.IsValidEmailAddress())
            .ToArray() ?? new string[0];

        public static string[] GetRoles(this ClaimsPrincipal principal, ClaimsConfiguration claimsConfig) =>
            claimsConfig.RoleClaimTypes?
            .Select(principal.FindAll)?
            .SelectMany(c => c.Select(claim => claim.Value.ToLower()))
            .Distinct()
            .ToArray() ?? new string[0];

        public static SeatRequest CreateSeatRequest(this ClaimsPrincipal principal, ClaimsConfiguration claimsConfig) =>
            new SeatRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                TenantId = principal.GetTenantId(claimsConfig),
                UserId = principal.GetUserId(claimsConfig),
                UserName = principal.GetUserName(claimsConfig),
                EmailAddresses = principal.GetEmails(claimsConfig).ToList(),
                Roles = principal.GetRoles(claimsConfig).ToList()
            };

        public static User ToCoreModel(this ClaimsPrincipal principal, ClaimsConfiguration claimsConfig) =>
            new User
            {
                Email = principal.GetEmails(claimsConfig).FirstOrDefault(),
                TenantId = principal.GetTenantId(claimsConfig),
                UserId = principal.GetUserId(claimsConfig),
                UserName = principal.GetUserName(claimsConfig)
            };

        public static bool CanUseSubscription(this ClaimsPrincipal principal, ClaimsConfiguration claimsConfig, Subscription subscription)
        {
            ArgumentNullException.ThrowIfNull(principal, nameof(principal));
            ArgumentNullException.ThrowIfNull(claimsConfig, nameof(claimsConfig));
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));

            return
                // Do they belong to the subscription's tenant?
                principal.GetTenantId(claimsConfig) == subscription.TenantId &&
                // If configured, do they belong to the subscription user's role?
                // By default, if you belong to the tenant, you can use the subscription.
                // If desired, a subscriber can configure a required role to use the subscription.
                (string.IsNullOrEmpty(subscription.UserRoleName) ||
                 principal.GetRoles(claimsConfig).Any(r => r.Equals(subscription.UserRoleName, StringComparison.OrdinalIgnoreCase)));
        }

        public static bool CanAdministerSubscription(this ClaimsPrincipal principal, ClaimsConfiguration claimsConfig, Subscription subscription)
        {
            ArgumentNullException.ThrowIfNull(principal, nameof(principal));
            ArgumentNullException.ThrowIfNull(claimsConfig, nameof(claimsConfig));
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));

            // So here are the subscription administration rules today.
            // First of all, they have to belong to the subscription's tenant...

            if (principal.GetTenantId(claimsConfig) == subscription.TenantId)
            {
                // If either the subscription admin role name or admin email is defined...

                if (!string.IsNullOrEmpty(subscription.AdminRoleName) ||
                    !string.IsNullOrEmpty(subscription.AdminEmail))
                {
                    // First check to see if they belong to the admin role...

                    if (!string.IsNullOrEmpty(subscription.AdminRoleName) &&
                        principal.GetRoles(claimsConfig).Any(r => r.Equals(subscription.AdminRoleName, StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }

                    // Then check to see if they have the admin email...

                    if (!string.IsNullOrEmpty(subscription.AdminEmail) &&
                        principal.GetEmails(claimsConfig).Contains(subscription.AdminEmail.ToLower()))
                    {
                        return true;
                    }

                    // If neither of these are true, then they're not an admin.
                }
                // However, if neither the role name or admin email is defined, then we give anyone within the tenant
                // access to administer the subscription. Here's the thing -- this should only happen when they first set up 
                // the subscription. During subscription setup, they need to provide an admin name and email.

                else
                {
                    return true;
                }
            }

            return false;
        }
    }
}
