namespace sernick.Utility;

public static class EnumerableExtensions
{
    /// <summary>
    /// If the enumerable is not null, returns it. Otherwise, returns empty enumerable
    /// </summary>
    public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T>? enumerable) => enumerable ?? Enumerable.Empty<T>();
}
