namespace sernick.Common.Regex;

internal sealed class StarRegex : Regex
{
    public StarRegex(Regex child)
    {
        Child = child;
    }

    public Regex Child { get; }

    public override bool ContainsEpsilon() => true;

    public override Regex Derivative(char atom)
    {
        return Concat(Child.Derivative(atom), this);
    }

    public override int GetHashCode()
    {
        return $"Star({Child.GetHashCode()})".GetHashCode();
    }

    public override bool Equals(Regex? other)
    {
        return other is StarRegex starRegex && Child.Equals(starRegex.Child);
    }
}

public partial class Regex
{
    public static partial Regex Star(Regex child)
    {
        // X^^ == X^
        if (child is StarRegex)
        {
            return child;
        }

        // \eps^ == \empty^ == \eps
        if (child.Equals(Epsilon) || child.Equals(Empty))
        {
            return Epsilon;
        }

        return new StarRegex(child);
    }
}
