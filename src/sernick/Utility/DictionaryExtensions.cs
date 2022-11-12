namespace sernick.Utility;

public static class DictionaryExtensions
{
    public static TValue GetOrAddEmpty<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        where TValue : new()
    {
        dictionary.TryAdd(key, new TValue());
        return dictionary[key];
    }
    
    public static IReadOnlyDictionary<K, V> JoinWith<K, V>(this IReadOnlyDictionary<K, V> dict, IReadOnlyDictionary<K, V> other) where K : notnull
    {
        return new []{dict, other}.SelectMany(d => d)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }
}
