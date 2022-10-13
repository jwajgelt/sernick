namespace sernick.Tokenizer.Regex;

internal class StarRegex : Regex
{
    public StarRegex(Regex child)
    {
        Child = child;
    }

    public Regex Child { get; }

    public override bool ContainsEpsilon()
    {
        throw new NotImplementedException();
    }

    public override Regex Derivative(char atom)
    {
        throw new NotImplementedException();
    }

    public override int GetHashCode()
    {
        return Child.GetHashCode();
    }

    public override bool Equals(Regex? other)
    {
        return other is StarRegex starRegex && Child.Equals(starRegex.Child);
    }
}
