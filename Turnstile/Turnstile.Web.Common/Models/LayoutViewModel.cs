﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Html;
using System.Security.Claims;
using Turnstile.Core.Models;
using Turnstile.Core.Models.Configuration;

namespace Turnstile.Web.Common.Models
{
    public class LayoutViewModel
    {
        public LayoutViewModel() { }

        public LayoutViewModel(PublisherConfiguration publisherConfig)
        {
            ArgumentNullException.ThrowIfNull(publisherConfig, nameof(publisherConfig));

            TurnstileName = publisherConfig.TurnstileName ?? "Turnstile";
            PublisherName = publisherConfig.PublisherName;
            HomePageUrl = publisherConfig.HomePageUrl;
            ContactPageUrl = publisherConfig.ContactPageUrl;
            PrivacyNoticePageUrl = publisherConfig.PrivacyNoticePageUrl;
            ContactSalesUrl = publisherConfig.ContactSalesUrl;
            ContactSupportUrl = publisherConfig.ContactSupportUrl;
            ContactSalesEmail = publisherConfig.ContactSalesEmail;
            ContactSupportEmail = publisherConfig.ContactSupportEmail;

            ContactSalesHtml = CreateContactSalesHtml(publisherConfig);
            ContactSupportHtml = CreateContactSupportHtml(publisherConfig);
        }

        public string? TurnstileName { get; set; }
        public string? PublisherName { get; set; }
        public string? HomePageUrl { get; set; }
        public string? ContactPageUrl { get; set; }
        public string? ContactSalesUrl { get; set; }
        public string? ContactSalesEmail { get; set; }
        public string? ContactSupportUrl { get; set; }
        public string? ContactSupportEmail { get; set; }
        public string? PrivacyNoticePageUrl { get; set; }

        public HtmlString? ContactSalesHtml { get; set; }
        public HtmlString? ContactSupportHtml { get; set; }

        public User? User { get; set; }

        public bool IsTurnstileAdmin { get; set; } = false;

        private HtmlString CreateContactSalesHtml(PublisherConfiguration publisherConfig)
        {
            if (!string.IsNullOrEmpty(publisherConfig.ContactSalesUrl))
            {
                return new HtmlString($@"<a href=""{publisherConfig.ContactSalesUrl}"" class=""alert-link"">visit our sales page</a>");
            }
            else if (!string.IsNullOrEmpty(publisherConfig.ContactSalesEmail))
            {
                return new HtmlString($@"<a href=""mailto:{publisherConfig.ContactSalesEmail}"" class=""alert-link"">contact sales</a>");
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
                return new HtmlString($@"<a href=""{publisherConfig.ContactSupportUrl}"" class=""alert-link"">visit our support page</a>");
            }
            else if (!string.IsNullOrEmpty(publisherConfig.ContactSupportEmail))
            {
                return new HtmlString($@"<a href=""mailto:{publisherConfig.ContactSupportEmail}"" class=""alert-link"">contact support</a>");
            }
            else
            {
                return new HtmlString("contact support");
            }
        }
    }
}
