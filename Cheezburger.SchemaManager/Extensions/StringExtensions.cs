using System;

namespace Cheezburger.SchemaManager.Extensions
{
    public static class StringExtensions
    {
        public static bool IsNullOrEmpty(this string value)
        {
            return String.IsNullOrEmpty(value);
        }

        public static bool IsUrl(this string value)
        {
            return !String.IsNullOrEmpty(value) && Uri.IsWellFormedUriString(value, UriKind.Absolute);
        }
    }
}