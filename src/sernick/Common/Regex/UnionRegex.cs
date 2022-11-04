namespace sernick.Common.Regex;

internal sealed class UnionRegex<TAtom> : Regex<TAtom> where TAtom : IEquatable<TAtom>
{
    public UnionRegex(IEnumerable<Regex<TAtom>> children)
    {
        Children = children.ToHashSet();
    }

    public IReadOnlySet<Regex<TAtom>> Children { get; }

    public override bool ContainsEpsilon() => Children.Any(child => child.ContainsEpsilon());

    public override Regex<TAtom> Derivative(TAtom atom)
    {
        return Children.Count == 0 ? Empty : Union(Children.Select(child => child.Derivative(atom)));
    }

    public override Regex<TAtom> Reverse()
    {
        return Union(Children.Select(child => child.Reverse()));
    }

    public override int GetHashCode()
    {
        return Children.Aggregate(Children.Count, (hashCode, child) => unchecked(hashCode + child.GetHashCode()));
    }

    public override bool Equals(Regex<TAtom>? other)
    {
        return other is UnionRegex<TAtom> unionRegex && Children.SetEquals(unionRegex.Children);
    }
}

public partial class Regex<TAtom> where TAtom : IEquatable<TAtom>
{
    public static partial Regex<TAtom> Union(IEnumerable<Regex<TAtom>> children)
    {
        IReadOnlyCollection<Regex<TAtom>> childrenList = children.ToList();

        // (X \cup Y) \cup Z == X \cup (Y \cup Z)
        childrenList = childrenList
            .SelectMany(regex =>
            {
                if (regex is UnionRegex<TAtom> unionRegex)
                {
                    return unionRegex.Children;
                }

                return Enumerable.Repeat(regex, count: 1);
            })
            .ToList();

        // \empty \cup X == X
        childrenList = childrenList.Where(regex => !regex.Equals(Empty)).ToList();

        // X \cup X == X
        IReadOnlyCollection<Regex<TAtom>> childrenSet = childrenList.ToHashSet();

        return childrenSet.Count switch
        {
            0 => Empty,
            1 => childrenSet.First(),
            _ => new UnionRegex<TAtom>(childrenSet)
        };
    }
}
