namespace sernick.Utility;

using System.Diagnostics.CodeAnalysis;

public static class DictionaryExtensions
{
    public static TValue GetOrAddEmpty<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        where TValue : new()
    {
        dictionary.TryAdd(key, new TValue());
        return dictionary[key];
    }

    public static IReadOnlyDictionary<K, V> JoinWith<K, V>(
        this IReadOnlyDictionary<K, V> dict,
        IReadOnlyDictionary<K, V> other,
        IEqualityComparer<K> comparer) where K : notnull
    {
        return new[] { dict, other }.SelectMany(d => d)
            .ToDictionary(pair => pair.Key, pair => pair.Value, comparer);
    }

    [return: NotNullIfNotNull("key")]
    public static TType? GetOrKey<TType>(this IDictionary<TType, TType> dictionary, TType? key)
    {
        return key != null && dictionary.TryGetValue(key, out var value) ? value : key;
    }
}
