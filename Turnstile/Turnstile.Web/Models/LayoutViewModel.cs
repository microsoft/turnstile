// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Html;
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

            ContactSalesHtml = CreateContactSalesHtml(publisherConfig);
            ContactSupportHtml = CreateContactSupportHtml(publisherConfig);
        }

        public string? TurnstileName { get; set; }
        public string? PublisherName { get; set; }
        public string? HomePageUrl { get; set; }
        public string? ContactPageUrl { get; set; }
        public string? ContactSalesUrl { get; set; }
        public string? ContactSupportUrl { get; set; }
        public string? PrivacyNoticePageUrl { get; set; }

        public HtmlString? ContactSalesHtml { get; set; }
        public HtmlString? ContactSupportHtml { get; set; }

        public bool IsTurnstileAdmin { get; set; } = false;

        private HtmlString CreateContactSalesHtml(PublisherConfiguration publisherConfig)
        {
            if (!string.IsNullOrEmpty(publisherConfig.ContactSalesUrl))
            {
                return new HtmlString($@"<a href=""{publisherConfig.ContactSalesUrl}"">visit our sales page</a>");
            }
            else if (!string.IsNullOrEmpty(publisherConfig.ContactSalesEmail))
            {
                return new HtmlString($@"<a href=""mailto:{publisherConfig.ContactSalesEmail}"">contact sales</a>");
            }
            else
            {
                return new HtmlString("contact sales");
            }
        }

        private HtmlString CreateContactSupportHtml(PublisherConfiguration publisherConfig)
        {
            if (!string.IsNullOrEmpty(publisherConfig.ContactSupportUrl))
            {
                return new HtmlString($@"<a href=""{publisherConfig.ContactSupportUrl}"">visit our support page</a>");
            }
            else if (!string.IsNullOrEmpty(publisherConfig.ContactSupportEmail))
            {
                return new HtmlString($@"<a href=""mailto:{publisherConfig.ContactSupportEmail}"">contact support</a>");
            }
            else
            {
                return new HtmlString("contact support");
            }
        }
    }
}
