namespace sernick.Common.Regex;

internal sealed class StarRegex<TAtom> : Regex<TAtom> where TAtom : IEquatable<TAtom>
{
    private readonly int _hash;
    public StarRegex(Regex<TAtom> child)
    {
        Child = child;
        _hash = $"Star({Child.GetHashCode()})".GetHashCode();
    }

    public Regex<TAtom> Child { get; }

    public override bool ContainsEpsilon() => true;

    public override Regex<TAtom> Derivative(TAtom atom)
    {
        return Concat(Child.Derivative(atom), this);
    }

    public override Regex<TAtom> Reverse() => Star(Child.Reverse());

    public override int GetHashCode() => _hash;

    public override bool Equals(Regex<TAtom>? other)
    {
        return ReferenceEquals(this, other) || (other is StarRegex<TAtom> starRegex && _hash == starRegex._hash && Child.Equals(starRegex.Child));
    }

    public override string ToString() => $"({Child})*";
}

public partial class Regex<TAtom> where TAtom : IEquatable<TAtom>
{
    public static partial Regex<TAtom> Star(Regex<TAtom> child)
    {
        // X^^ == X^
        if (child is StarRegex<TAtom>)
        {
            return child;
        }

        // \eps^ == \empty^ == \eps
        if (child.Equals(Epsilon) || child.Equals(Empty))
        {
            return Epsilon;
        }

        return new StarRegex<TAtom>(child);
    }
}
