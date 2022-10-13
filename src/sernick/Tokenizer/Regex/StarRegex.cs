namespace sernick.Tokenizer.Regex;

internal sealed class StarRegex : Regex
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
        throw new NotImplementedException();
    }

    public override bool Equals(Regex? other)
    {
        throw new NotImplementedException();
    }
}

partial class Regex
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
