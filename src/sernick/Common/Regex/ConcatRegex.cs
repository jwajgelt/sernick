namespace sernick.Common.Regex;

using Utility;

internal sealed class ConcatRegex<TAtom> : Regex<TAtom> where TAtom : IEquatable<TAtom>
{
    private readonly int _hash;
    public ConcatRegex(IEnumerable<Regex<TAtom>> children)
    {
        Children = children.ToList();
        _hash = Children.GetCombinedHashCode();
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
        return Concat(Children.Reverse().Select(child => child.Reverse()));
    }

    public override int GetHashCode() => _hash;

    public override bool Equals(Regex<TAtom>? other)
    {
        return ReferenceEquals(this, other) || (other is ConcatRegex<TAtom> concatRegex && _hash == concatRegex._hash && Children.SequenceEqual(concatRegex.Children));
    }

    public override string ToString() => string.Join("*", Children);
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

                return regex.Enumerate();
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
