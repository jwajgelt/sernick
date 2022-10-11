using System.Collections;

namespace sernick.Tokenizer.Regex;
public abstract class Regex : IEqualityComparer<Regex>
{
    public static Regex Atom(char character)
    {
        throw new NotImplementedException();
    }

    public static Regex Union(IEnumerable<Regex> children)
    {
        throw new NotImplementedException();
    }

    public static Regex Concat(IEnumerable<Regex> children)
    {
        throw new NotImplementedException();
    }

    public static Regex Star(Regex child)
    {
        throw new NotImplementedException();
    }

    public abstract bool ContainsEpsilon();
    public abstract Regex Derivative();
    public abstract bool Equals(Regex? x, Regex? y);
    public abstract int GetHashCode(Regex obj);
}