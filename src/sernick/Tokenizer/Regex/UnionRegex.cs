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
        return Children.Any(child => child.ContainsEpsilon());
    }

    public override Regex Derivative(char atom)
    {
        if (Children.Count == 0)
        {
            return Empty;
        }

        return Union(Children.Select(child => child.Derivative(atom)));
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

        // X \cup X == X
        IReadOnlyCollection<Regex> childrenSet = childrenList.ToHashSet();

        return childrenSet.Count switch
        {
            0 => Empty,
            1 => childrenSet.First(),
            _ => new UnionRegex(childrenSet)
        };
    }
}
