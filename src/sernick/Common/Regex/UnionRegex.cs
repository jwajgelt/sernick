namespace sernick.Common.Regex;

using Utility;

internal sealed class UnionRegex<TAtom> : Regex<TAtom> where TAtom : IEquatable<TAtom>
{
    private readonly int _hash;
    public UnionRegex(IEnumerable<Regex<TAtom>> children)
    {
        Children = children.ToHashSet();
        _hash = Children.GetCombinedSetHashCode();
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

    public override int GetHashCode() => _hash;

    public override bool Equals(Regex<TAtom>? other)
    {
        return ReferenceEquals(this, other) || (other is UnionRegex<TAtom> unionRegex && _hash == unionRegex._hash && Children.SetEquals(unionRegex.Children));
    }

    public override string ToString() => string.Join("+", Children);
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
