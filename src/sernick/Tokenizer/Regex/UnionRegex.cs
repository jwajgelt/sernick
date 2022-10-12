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
        throw new NotImplementedException();
    }

    public override bool Equals(Regex? other)
    {
        throw new NotImplementedException();
    }
}

partial class Regex
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
