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

    public static IDictionary<K, V> MergeWith<K, V>(
        this IDictionary<K, V> dict,
        IDictionary<K, V> other)
    {
        foreach (var (key, value) in other)
        {
            dict[key] = value;
        }

        return dict;
    }
}
