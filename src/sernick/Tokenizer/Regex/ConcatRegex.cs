namespace sernick.Tokenizer.Regex;

internal class ConcatRegex : Regex
{
    public ConcatRegex(IEnumerable<Regex> children)
    {
        Children = new List<Regex>(children);
    }

    public IReadOnlyList<Regex> Children { get; }

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
