namespace sernick.Common.Regex;

public abstract partial class Regex<TAtom> : IEquatable<Regex<TAtom>>
    where TAtom : IEquatable<TAtom>
{
    public static partial Regex<TAtom> Atom(TAtom character);
    public static partial Regex<TAtom> Union(IEnumerable<Regex<TAtom>> children);
    public static partial Regex<TAtom> Concat(IEnumerable<Regex<TAtom>> children);
    public static partial Regex<TAtom> Star(Regex<TAtom> child);

    public static Regex<TAtom> Union(params Regex<TAtom>[] children) => Union(children.AsEnumerable());
    public static Regex<TAtom> Concat(params Regex<TAtom>[] children) => Concat(children.AsEnumerable());

    public static readonly Regex<TAtom> Empty = new UnionRegex<TAtom>(Enumerable.Empty<Regex<TAtom>>());
    public static readonly Regex<TAtom> Epsilon = new StarRegex<TAtom>(Empty);

    public abstract override int GetHashCode();
    public abstract bool Equals(Regex<TAtom>? other);
    public abstract bool ContainsEpsilon();
    public abstract Regex<TAtom> Derivative(TAtom atom);

    public override bool Equals(object? obj)
    {
        return obj is Regex<TAtom> regex && Equals(regex);
    }
}
