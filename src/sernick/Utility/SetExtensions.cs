namespace sernick.Utility;

public static class SetExtensions
{
    public static bool UnionWithCheck<T>(this ISet<T> set, IEnumerable<T> other)
    {
        var count = set.Count;
        set.UnionWith(other);
        return count != set.Count;
    }
}
