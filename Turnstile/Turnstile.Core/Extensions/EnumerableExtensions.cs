namespace Turnstile.Core.Extensions
{
    public static class EnumerableExtensions
    {
        public static bool None<T>(this IEnumerable<T> inSet) =>
            inSet?.Any() == false;

        public static bool OnlyOne<T>(this IEnumerable<T> inSet) =>
            inSet?.Count() == 1;
    }
}
