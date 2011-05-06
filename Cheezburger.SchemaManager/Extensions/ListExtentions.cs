// Copyright (C) 2011 by Cheezburger, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

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
