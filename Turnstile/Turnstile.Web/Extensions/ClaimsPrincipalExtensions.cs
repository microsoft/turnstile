using Microsoft.Identity.Web;
using System.Security.Claims;
using Turnstile.Core.Models;
using static System.Environment;
using static Turnstile.Core.Constants.EnvironmentVariableNames;

namespace Turnstile.Web.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static User ToSubscriptionUser(this ClaimsPrincipal principal)
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

        public static IEnumerable<string> GetEmailAddresses(this ClaimsPrincipal principal)
        {
            ArgumentNullException.ThrowIfNull(principal, nameof(principal));

            return principal.Claims.Where(c => c.Type == ClaimTypes.Email).Select(c => c.Value.ToLower());
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

            return
                // Do they belong to the subscription's tenant?
                principal.GetHomeTenantId() == subscription.TenantId &&
                // Do they belong to the subscription administrator's role?
                // There should _always_ be an admin role name.
                // Just letting any member of the tenant administer the subscription seems dangerous.
                // Even the publisher can't directly administer a customer's subscription unless, of course, 
                // the subscription's tenant ID == the publisher's tenant ID (e.g., in a testing scenario.)
                principal.IsInRole(subscription.AdminRoleName!);
        }
    }
}
