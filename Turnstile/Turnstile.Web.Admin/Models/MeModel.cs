using System.Security.Claims;
using Turnstile.Core.Models.Configuration;
using Turnstile.Web.Common.Extensions;

namespace Turnstile.Web.Admin.Models
{
    public class MeModel
    {
        public MeModel() { }

        public MeModel(ClaimsPrincipal principal, ClaimsConfiguration claimsConfig)
        {
            ArgumentNullException.ThrowIfNull(principal, nameof(principal));
            ArgumentNullException.ThrowIfNull(claimsConfig, nameof(claimsConfig));

            UserId = principal.GetUserId(claimsConfig);
            UserName = principal.GetUserName(claimsConfig);
            TenantId = principal.GetTenantId(claimsConfig);
            Emails = principal.GetEmails(claimsConfig).ToList();
            Roles = principal.GetRoles(claimsConfig).ToList();
        }

        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? TenantId { get; set; }

        public List<string> Emails { get; set; } = new List<string>();
        public List<string> Roles { get; set; } = new List<string>();
    }
}
