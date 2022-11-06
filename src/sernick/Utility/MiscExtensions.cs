namespace sernick.Utility;

public static class MiscExtensions
{
    /// <summary>
    /// Moves enumerator to the next position and returns the value.
    /// </summary>
    /// <returns>Value at the next position if the position is valid; otherwise - default(T)</returns>
    public static T? Next<T>(this IEnumerator<T> enumerator) => enumerator.MoveNext() ? enumerator.Current : default;
}
