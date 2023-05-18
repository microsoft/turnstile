using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Turnstile.Web.Common.Models;

namespace Turnstile.Web.Common.Extensions
{
    public static class ViewDataDictionaryExtensions
    {
        public static class ViewDataKeys
        {
            public const string Layout = nameof(LayoutViewModel);
            public const string SubscriptionContext = nameof(SubscriptionContextViewModel);
            public const string SeatingContext = nameof(SubscriptionSeatingViewModel);
        }

        public static LayoutViewModel? GetLayoutModel(this ViewDataDictionary viewData) =>
            viewData[ViewDataKeys.Layout] as LayoutViewModel;

        public static SubscriptionContextViewModel? GetSubscriptionContextModel(this ViewDataDictionary viewData) =>
            viewData[ViewDataKeys.SubscriptionContext] as SubscriptionContextViewModel;

        public static SubscriptionSeatingViewModel? GetSeatingModel(this ViewDataDictionary viewData) =>
            viewData[ViewDataKeys.SeatingContext] as SubscriptionSeatingViewModel;

        public static void ApplyModel(this Controller controller, LayoutViewModel model) =>
            controller.ViewData[ViewDataKeys.Layout] = model;

        public static void ApplyModel(this Controller controller, SubscriptionContextViewModel model) =>
            controller.ViewData[ViewDataKeys.SubscriptionContext] = model;

        public static void ApplyModel(this Controller controller, SubscriptionSeatingViewModel model) =>
            controller.ViewData[ViewDataKeys.SeatingContext] = model;
    }
}
