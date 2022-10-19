namespace sernick.Common.Regex;

public abstract partial class Regex : IEquatable<Regex>
{
    public static partial Regex Atom(char character);
    public static partial Regex Union(IEnumerable<Regex> children);
    public static partial Regex Concat(IEnumerable<Regex> children);
    public static partial Regex Star(Regex child);

    public static Regex Union(params Regex[] children) => Union(children.AsEnumerable());
    public static Regex Concat(params Regex[] children) => Concat(children.AsEnumerable());

    public static readonly Regex Empty = new UnionRegex(Enumerable.Empty<Regex>());
    public static readonly Regex Epsilon = new StarRegex(Empty);

    public abstract override int GetHashCode();
    public abstract bool Equals(Regex? other);
    public abstract bool ContainsEpsilon();
    public abstract Regex Derivative(char atom);

    public override bool Equals(object? obj)
    {
        return obj is Regex regex && Equals(regex);
    }
}
