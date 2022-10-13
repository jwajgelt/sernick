namespace sernick.Tokenizer.Regex;

internal sealed class ConcatRegex : Regex
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

public partial class Regex
{
    public static partial Regex Concat(IEnumerable<Regex> children)
    {
        IReadOnlyCollection<Regex> childrenList = children.ToList();

        // (X * Y) * Z == X * (Y * Z)
        childrenList = childrenList
            .SelectMany(regex =>
            {
                if (regex is ConcatRegex concatRegex)
                {
                    return concatRegex.Children;
                }

                return Enumerable.Repeat(regex, count: 1);
            })
            .ToList();

        // \empty * X == X * \empty == \empty
        if (childrenList.Any(regex => regex.Equals(Empty)))
        {
            return Empty;
        }

        // \eps * X == X * \eps == X
        childrenList = childrenList.Where(regex => !regex.Equals(Epsilon)).ToList();

        return childrenList.Count switch
        {
            0 => Epsilon,
            1 => childrenList.First(),
            _ => new ConcatRegex(childrenList)
        };
    }
}
