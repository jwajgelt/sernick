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

    public override Regex Derivative(char atom)
    {
        throw new NotImplementedException();
    }

    public override int GetHashCode()
    {
        var hashCode = Children.Count;
        foreach (var child in Children)
        {
            hashCode = unchecked(hashCode * 17 + child.GetHashCode());
        }

        return hashCode;
    }

    public override bool Equals(Regex? other)
    {
        return other is ConcatRegex concatRegex && Children.SequenceEqual(concatRegex.Children);
    }
}
