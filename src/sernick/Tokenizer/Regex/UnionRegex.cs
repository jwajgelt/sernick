namespace sernick.Tokenizer.Regex;

internal class UnionRegex : Regex
{
    public UnionRegex(IEnumerable<Regex> children)
    {
        Children = new HashSet<Regex>(children);
    }

    public IReadOnlySet<Regex> Children { get; }

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
