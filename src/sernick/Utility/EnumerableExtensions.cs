namespace sernick.Utility;

public static class EnumerableExtensions
{
    /// <summary>
    /// If the enumerable is not null, returns it. Otherwise, returns empty enumerable
    /// </summary>
    public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T>? enumerable) => enumerable ?? Enumerable.Empty<T>();

    /// <summary>
    /// Creates enumerable with a single element <see cref="t"/>
    /// </summary>
    public static IEnumerable<T> Enumerate<T>(this T t)
    {
        yield return t;
    }
}
