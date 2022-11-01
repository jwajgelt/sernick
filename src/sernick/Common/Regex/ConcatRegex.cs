namespace sernick.Common.Regex;

internal sealed class ConcatRegex<TAtom> : Regex<TAtom> where TAtom : IEquatable<TAtom>
{
    public ConcatRegex(IEnumerable<Regex<TAtom>> children)
    {
        Children = children.ToList();
    }

    public IReadOnlyList<Regex<TAtom>> Children { get; }

    public override bool ContainsEpsilon()
    {
        return Children.All(child => child.ContainsEpsilon());
    }

    public override Regex<TAtom> Derivative(TAtom atom)
    {
        if (Children.Count == 0)
        {
            return Empty;
        }

        var firstRegex = Children[0];
        var firstDerivative = firstRegex.Derivative(atom);
        var epsilonLessDerivative = Concat(Children.Skip(1).Prepend(firstDerivative));

        return firstRegex.ContainsEpsilon()
            ? Union(
                epsilonLessDerivative,
                Concat(Children.Skip(1)).Derivative(atom)
            )
            : epsilonLessDerivative;
    }

    public override Regex<TAtom> Reverse()
    {
        return Children.Count == 0 ? Empty : Concat(Children.Reverse().Select(child => child.Reverse()));
    }

    public override int GetHashCode()
    {
        return Children.Aggregate(
            Children.Count,
            (hashCode, child) => unchecked(hashCode * 17 + child.GetHashCode())
            );
    }

    public override bool Equals(Regex<TAtom>? other)
    {
        return other is ConcatRegex<TAtom> concatRegex && Children.SequenceEqual(concatRegex.Children);
    }
}

public partial class Regex<TAtom> where TAtom : IEquatable<TAtom>
{
    public static partial Regex<TAtom> Concat(IEnumerable<Regex<TAtom>> children)
    {
        IReadOnlyCollection<Regex<TAtom>> childrenList = children.ToList();

        // (X * Y) * Z == X * (Y * Z)
        childrenList = childrenList
            .SelectMany(regex =>
            {
                if (regex is ConcatRegex<TAtom> concatRegex)
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
            _ => new ConcatRegex<TAtom>(childrenList)
        };
    }
}
