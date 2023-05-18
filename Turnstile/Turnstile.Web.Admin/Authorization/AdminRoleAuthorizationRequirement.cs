using Microsoft.AspNetCore.Authorization;

namespace Turnstile.Web.Admin.Authorization
{
    public class AdminRoleAuthorizationRequirement : IAuthorizationRequirement
    {
        public AdminRoleAuthorizationRequirement(string adminRoleName) =>
            AdminRoleName = adminRoleName ?? throw new ArgumentNullException(nameof(adminRoleName));

        public string AdminRoleName { get; set; }
    }
}
