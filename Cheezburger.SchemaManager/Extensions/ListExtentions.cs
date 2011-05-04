using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cheezburger.SchemaManager.Extensions
{
    public static class ListExtentions
    {
        public static T[] ToArrayOrNull<T>(this IEnumerable<T> src)
        {
            var arr = src.ToArray();
            if (arr.Length == 0)
                return null;
            return arr;
        }

        public static string Join(this string[] src, string delimiter)
        {
            return string.Join(delimiter, src);
        }

        public static string Join(this IEnumerable<string> src, string delimiter)
        {
            var sb = new StringBuilder();
            Join(src, delimiter, sb);
            return sb.ToString();
        }

        public static void Join(this IEnumerable<string> src, string delimiter, StringBuilder sb)
        {
            using (var it = src.GetEnumerator())
            {
                if (!it.MoveNext())
                    return;

                sb.Append(it.Current);

                while (it.MoveNext())
                {
                    sb.Append(delimiter);
                    sb.Append(it.Current);
                }
            }
        }

        public static IEnumerable<KeyValuePair<int, T>> WithIndexes<T>(this IEnumerable<T> src)
        {
            var ix = 0;
            return src.Select(t => new KeyValuePair<int, T>(ix++, t));
        }
    }
}
