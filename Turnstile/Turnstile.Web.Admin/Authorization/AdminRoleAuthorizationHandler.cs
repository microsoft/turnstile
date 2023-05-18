using Microsoft.AspNetCore.Authorization;

namespace Turnstile.Web.Admin.Authorization
{
    public class AdminRoleAuthorizationHandler : AuthorizationHandler<AdminRoleAuthorizationRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRoleAuthorizationRequirement requirement)
        {
            if (context.User.IsInRole(requirement.AdminRoleName))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
