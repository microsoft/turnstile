// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Identity.Web;
using System.Security.Claims;
using Turnstile.Core.Models;
using static System.Environment;
using static Turnstile.Core.Constants.EnvironmentVariableNames;

namespace Turnstile.Web.Extensions
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

        public static SeatRequest CreateSeatRequest(this ClaimsPrincipal principal) =>
            new SeatRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                TenantId = principal.GetHomeTenantId(),
                UserId = principal.GetHomeObjectId(),
                UserName = principal.GetDisplayName(), 
                EmailAddresses = principal.Claims.Where(c => c.Type == ClaimTypes.Email).Select(c => c.Value.ToLower()).ToList(),
                Roles = principal.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value.ToLower()).ToList()
            };

        public static User ToCoreModel(this ClaimsPrincipal principal)
        {
            ArgumentNullException.ThrowIfNull(principal, nameof(principal));

            return new User
            {
                Email = principal.FindFirstValue(ClaimTypes.Email),
                TenantId = principal.GetHomeTenantId(),
                UserId = principal.GetHomeObjectId(),
                UserName = principal.GetDisplayName()
            };
        }

        public static bool CanAdministerTurnstile(this ClaimsPrincipal principal)
        {
            ArgumentNullException.ThrowIfNull(principal, nameof(principal));

            var pubTenantId = GetEnvironmentVariable(Publisher.TenantId) ??
                throw new InvalidOperationException($"[{Publisher.TenantId}] environment variable not configured.");

            var pubAdminRoleName = GetEnvironmentVariable(Publisher.AdminRoleName) ??
                throw new InvalidOperationException($"[{Publisher.AdminRoleName}] environment variable not configured.");

            return 
                // Do they belong to this turnstile's tenant?
                principal.GetHomeTenantId() == pubTenantId && 
                // Do they belong to the administrator's role?
                principal.IsInRole(pubAdminRoleName);
        }

        public static bool CanUseSubscription(this ClaimsPrincipal principal, Subscription subscription)
        {
            ArgumentNullException.ThrowIfNull(principal, nameof(principal));
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));

            return
                // Do they belong to the subscription's tenant?
                principal.GetHomeTenantId() == subscription.TenantId &&
                // If configured, do they belong to the subscription user's role?
                // By default, if you belong to the tenant, you can use the subscription.
                // If desired, a subscriber can configure a required role to use the subscription.
                (string.IsNullOrEmpty(subscription.UserRoleName) || principal.IsInRole(subscription.UserRoleName));
        }

        public static bool CanAdministerSubscription(this ClaimsPrincipal principal, Subscription subscription)
        {
            ArgumentNullException.ThrowIfNull(principal, nameof(principal));
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));

            // So here are the subscription administration rules today.
            // First of all, they have to belong to the subscription's tenant...

            if (principal.GetHomeTenantId() == subscription.TenantId)
            {
                // If either the subscription admin role name or admin email is defined...

                if (!string.IsNullOrEmpty(subscription.AdminRoleName) ||
                    !string.IsNullOrEmpty(subscription.AdminEmail))
                {
                    // First check to see if they belong to the admin role...

                    if (!string.IsNullOrEmpty(subscription.AdminRoleName) &&
                        principal.IsInRole(subscription.AdminRoleName!))
                    {
                        return true;
                    }

                    // Then check to see if they have the admin email...

                    if (!string.IsNullOrEmpty(subscription.AdminEmail) &&
                        principal.Claims
                            .Where(c => c.Type == ClaimTypes.Email)
                            .Select(c => c.Value.ToLower())
                            .Contains(subscription.AdminEmail.ToLower()))
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
