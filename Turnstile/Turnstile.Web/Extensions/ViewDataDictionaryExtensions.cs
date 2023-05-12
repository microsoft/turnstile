using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Turnstile.Web.Models;

namespace Turnstile.Web.Extensions
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

        public static void ApplyModel(this ViewDataDictionary viewData, LayoutViewModel model) =>
            viewData[ViewDataKeys.Layout] = model;

        public static void ApplyModel(this ViewDataDictionary viewData, SubscriptionContextViewModel model) =>
            viewData[ViewDataKeys.SubscriptionContext] = model;

        public static void ApplyModel(this ViewDataDictionary viewData, SubscriptionSeatingViewModel model) =>
            viewData[ViewDataKeys.SeatingContext] = model;
    }
}
