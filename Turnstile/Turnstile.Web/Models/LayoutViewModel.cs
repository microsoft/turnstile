// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Claims;
using Turnstile.Core.Models.Configuration;
using Turnstile.Web.Extensions;

namespace Turnstile.Web.Models
{
    public class LayoutViewModel
    {
        public LayoutViewModel() { }

        public LayoutViewModel(PublisherConfiguration publisherConfig, ClaimsPrincipal? forPrincipal)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));

            TurnstileName = publisherConfig.TurnstileName;
            PublisherName = publisherConfig.PublisherName;
            HomePageUrl = publisherConfig.HomePageUrl;
            ContactPageUrl = publisherConfig.ContactPageUrl;
            PrivacyNoticePageUrl = publisherConfig.PrivacyNoticePageUrl;
            ContactSalesUrl = publisherConfig.ContactSalesUrl;
            ContactSupportUrl = publisherConfig.ContactSupportUrl;
            IsTurnstileAdmin = (forPrincipal?.CanAdministerTurnstile() == true);
        }

        public string? TurnstileName { get; set; }
        public string? PublisherName { get; set; }
        public string? HomePageUrl { get; set; }
        public string? ContactPageUrl { get; set; }
        public string? ContactSalesUrl { get; set; }
        public string? ContactSupportUrl { get; set; }
        public string? PrivacyNoticePageUrl { get; set; }

        public bool IsTurnstileAdmin { get; set; } = false;
    }
}
