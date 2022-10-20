namespace sernick.Common.Regex;

internal sealed class StarRegex<TAtom> : Regex<TAtom> where TAtom : IEquatable<TAtom>
{
    public StarRegex(Regex<TAtom> child)
    {
        Child = child;
    }

    public Regex<TAtom> Child { get; }

    public override bool ContainsEpsilon() => true;

    public override Regex<TAtom> Derivative(TAtom atom)
    {
        return Concat(Child.Derivative(atom), this);
    }

    public override int GetHashCode()
    {
        return $"Star({Child.GetHashCode()})".GetHashCode();
    }

    public override bool Equals(Regex<TAtom>? other)
    {
        return other is StarRegex<TAtom> starRegex && Child.Equals(starRegex.Child);
    }
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
