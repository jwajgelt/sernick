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
        IEqualityComparer<K>? comparer = null) where K : notnull
    {
        return new[] { dict, other }.SelectMany(d => d)
            .ToDictionary(pair => pair.Key, pair => pair.Value, comparer);
    }

    public static IReadOnlyDictionary<K, V> JoinWithOverlapping<K, V>(
        this IReadOnlyDictionary<K, V> dict,
        IReadOnlyDictionary<K, V> other) where K : notnull
    {
        return new[] { dict, other }.SelectMany(d => d).Distinct()
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    public static IEnumerable<TResult> Select<TSourceKey, TSourceValue, TResult>(
        this IEnumerable<KeyValuePair<TSourceKey, TSourceValue>> source,
        Func<TSourceKey, TSourceValue, TResult> selector)
    {
        return source.Select(kv => selector(kv.Key, kv.Value));
    }

    public static IEnumerable<TResult> SelectMany<TSourceKey, TSourceValue, TResult>(
        this IEnumerable<KeyValuePair<TSourceKey, TSourceValue>> source,
        Func<TSourceKey, TSourceValue, IEnumerable<TResult>> selector)
    {
        return source.SelectMany(kv => selector(kv.Key, kv.Value));
    }

    /// <summary>
    /// Filters the dictionary to only entries, whose value has type <typeparamref name="V2"/>
    /// </summary>
    public static IReadOnlyDictionary<K, V2> ValuesOfType<V2, K, V1>(this IEnumerable<KeyValuePair<K, V1>> dict)
        where K : notnull
        where V1 : notnull
        where V2 : V1
    {
        return dict
            .Where(kv => kv.Value is V2)
            .ToDictionary(kv => kv.Key, kv => (V2)kv.Value);
    }

    [return: NotNullIfNotNull("key")]
    public static TType? GetOrKey<TType>(this IReadOnlyDictionary<TType, TType> dictionary, TType? key)
    {
        return key != null && dictionary.TryGetValue(key, out var value) ? value : key;
    }
}
