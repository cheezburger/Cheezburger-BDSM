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
