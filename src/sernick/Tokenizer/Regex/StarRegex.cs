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

    public override Regex Derivative()
    {
        throw new NotImplementedException();
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }

    public override bool Equals(Regex? other)
    {
        throw new NotImplementedException();
    }
}