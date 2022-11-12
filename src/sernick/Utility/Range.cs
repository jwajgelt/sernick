namespace sernick.Utility;

public record Range<T>(T Start, T End)
{
    public static implicit operator Range<T>((T start, T end) range) => new(range.start, range.end);
}
