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

using System;
using System.Collections.Generic;

namespace Cheezburger.SchemaManager.Extensions
{
    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
        {
            TValue val;
            if (!dict.TryGetValue(key, out val))
                return defaultValue;
            return val;
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
        {
            return dict.GetValueOrDefault(key, default(TValue));
        }

        /// <summary>
        /// If the dictionary key is present, get the dictionary value at that key. Otherwise, add onMissing at that dictionary key and return the new dictionary value.
        /// </summary>
        /// <typeparam name="TKey">Type of the dictionary key</typeparam>
        /// <typeparam name="TValue">Type of the dictionary value</typeparam>
        /// <param name="dict">The dictionary</param>
        /// <param name="key">The dictionary key</param>
        /// <param name="onMissing">Will be called if the value is missing: should return the new dictionaryvalue for the missing dictionary key</param>
        public static TValue GetValue<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> onMissing)
        {
            TValue val;
            if (!dict.TryGetValue(key, out val))
                return (dict[key] = onMissing());
            return val;
        }

        public static void Add<TKey, TValue>(this ICollection<KeyValuePair<TKey, TValue>> collection, TKey key, TValue val)
        {
            collection.Add(new KeyValuePair<TKey, TValue>(key, val));
        }
        /// <summary>
        /// Merges two dictionaries together. When both dictionaries contain the same key, onConflict is called to resolve the value that should be used.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dict"></param>
        /// <param name="other"></param>
        /// <param name="onConflict"></param>
        /// <returns></returns>
        public static IDictionary<TKey, TValue> Merge<TKey, TValue>(this IDictionary<TKey, TValue> dict, IDictionary<TKey, TValue> other, Func<TKey, TValue, TValue, TValue> onConflict)
        {
            var result = new Dictionary<TKey, TValue>(dict);
            foreach (var kvp in other)
                if (result.ContainsKey(kvp.Key))
                    result[kvp.Key] = onConflict(kvp.Key, result[kvp.Key], kvp.Value);
                else
                    result.Add(kvp.Key, kvp.Value);
            return result;
        }
    }
}
