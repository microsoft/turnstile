namespace Turnstile.Core.Constants
{
    public static class SeatTypes
    {
        public const string Standard = "standard";
        public const string Limited = "limited";

        public static readonly string[] ValidTypes =
            new[] { Standard, Limited };
    }
}
