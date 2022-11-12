namespace sernick.Utility;

public static class MiscExtensions
{
    /// <summary>
    /// Moves enumerator to the next position and returns the value.
    /// </summary>
    /// <returns>Value at the next position if the position is valid; otherwise - default(T)</returns>
    public static T? Next<T>(this IEnumerator<T> enumerator) => enumerator.MoveNext() ? enumerator.Current : default;
    
    /// <summary>
    /// Default `GetHashCode` for collections is inherited from `object.GetHashCode()`, which usually isn't desirable.
    /// <b>Order of elements matters</b>
    /// </summary>
    /// <returns>Combined hash code of the contents</returns>
    public static int GetCombinedHashCode<T>(this IEnumerable<T> source) => 
        source.Aggregate(typeof(T).GetHashCode(), HashCode.Combine);
    
    /// <summary>
    /// Default `GetHashCode` for collections is inherited from `object.GetHashCode()`, which usually isn't desirable.
    /// <b>Order of elements does not matter</b>
    /// </summary>
    /// <returns>Combined hash code of the contents</returns>
    public static int GetCombinedSetHashCode<T>(this IReadOnlyCollection<T> source) where T : notnull => 
        source.Aggregate(HashCode.Combine(typeof(T), source.Count), (hashCode, t) => hashCode ^ t.GetHashCode());
}
