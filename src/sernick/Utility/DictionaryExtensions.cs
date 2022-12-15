namespace sernick.Utility;

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
}
