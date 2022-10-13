namespace sernick.Tokenizer.Regex;

internal sealed class UnionRegex : Regex
{
    public UnionRegex(IEnumerable<Regex> children)
    {
        Children = children.ToHashSet();
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
        var hashCode = Children.Count;
        foreach (var child in Children)
        {
            hashCode = unchecked(hashCode + child.GetHashCode());
        }

        return hashCode;
    }

    public override bool Equals(Regex? other)
    {
        return other is UnionRegex unionRegex && Children.SetEquals(unionRegex.Children);
    }
}

public partial class Regex
{
    public static partial Regex Union(IEnumerable<Regex> children)
    {
        IReadOnlyCollection<Regex> childrenList = children.ToList();

        // (X \cup Y) \cup Z == X \cup (Y \cup Z)
        childrenList = childrenList
            .SelectMany(regex =>
            {
                if (regex is UnionRegex unionRegex)
                {
                    return unionRegex.Children;
                }

                return Enumerable.Repeat(regex, count: 1);
            })
            .ToList();

        // \empty \cup X == X
        childrenList = childrenList.Where(regex => !regex.Equals(Empty)).ToList();

        return childrenList.Count switch
        {
            0 => Empty,
            1 => childrenList.First(),
            _ => new UnionRegex(childrenList)
        };
    }
}
