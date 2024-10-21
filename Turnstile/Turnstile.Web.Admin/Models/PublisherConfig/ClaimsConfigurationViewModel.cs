using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using Turnstile.Core.Models.Configuration;

namespace Turnstile.Web.Admin.Models.PublisherConfig
{
    public class ClaimsConfigurationViewModel : BaseConfigurationViewModel
    {
        public ClaimsConfigurationViewModel() { }

        public ClaimsConfigurationViewModel(ClaimsConfiguration? claimsConfig)
        {
            if (claimsConfig is not null)
            {
                UserIdClaimTypes =
                    claimsConfig.UserIdClaimTypes is not null ?
                    string.Join(", ", claimsConfig.UserIdClaimTypes) : null;

                UserNameClaimTypes =
                    claimsConfig.UserNameClaimTypes is not null ?
                    string.Join(", ", claimsConfig.UserNameClaimTypes) : null;

                TenantIdClaimTypes =
                    claimsConfig.TenantIdClaimTypes is not null ?
                    string.Join(", ", claimsConfig.TenantIdClaimTypes) : null;

                EmailClaimTypes =
                    claimsConfig.EmailClaimTypes is not null ?
                    string.Join(", ", claimsConfig.EmailClaimTypes) : null;

                RoleClaimTypes =
                    claimsConfig.RoleClaimTypes is not null ?
                    string.Join(", ", claimsConfig.RoleClaimTypes) : null;
            }
        }

        [Display(Name = "User ID claim types")]
        [Required(ErrorMessage = "User ID claim types are required.")]
        public string? UserIdClaimTypes { get; set; }

        [Display(Name = "User name claim types")]
        [Required(ErrorMessage = "User name claim types are required.")]
        public string? UserNameClaimTypes { get; set; }

        [Display(Name = "Tenant ID claim types")]
        [Required(ErrorMessage = "Tenant ID claim types are required.")]
        public string? TenantIdClaimTypes { get; set; }

        [Display(Name = "Email claim types")]
        [Required(ErrorMessage = "Email claim types are required.")]
        public string? EmailClaimTypes { get; set; }

        [Display(Name = "Role claim types")]
        [Required(ErrorMessage = "Role claim types are required.")]
        public string? RoleClaimTypes { get; set; }
    }
}
