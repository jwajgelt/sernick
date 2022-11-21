namespace sernick.Common.Regex;

internal sealed class AtomRegex<TAtom> : Regex<TAtom> where TAtom : IEquatable<TAtom>
{
    public AtomRegex(TAtom atom)
    {
        Atom = atom;
    }

    public new TAtom Atom { get; }

    public override bool ContainsEpsilon() => false;

    public override Regex<TAtom> Derivative(TAtom atom) => Atom.Equals(atom) ? Epsilon : Empty;

    public override Regex<TAtom> Reverse() => Atom(Atom);

    public override int GetHashCode() => Atom.GetHashCode();

    public override bool Equals(Regex<TAtom>? other)
    {
        return ReferenceEquals(this, other) || (other is AtomRegex<TAtom> atomRegex && Atom.Equals(atomRegex.Atom));
    }

    public override string? ToString() => Atom.ToString();
}

public partial class Regex<TAtom> where TAtom : IEquatable<TAtom>
{
    public static partial Regex<TAtom> Atom(TAtom atom) => new AtomRegex<TAtom>(atom);
}
