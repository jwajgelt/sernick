namespace sernick.Tokenizer.Regex;
public abstract class Regex : IEquatable<Regex>
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

    public abstract override int GetHashCode();
    public abstract bool Equals(Regex? other);
    public abstract bool ContainsEpsilon();
    public abstract Regex Derivative(char atom);

    public override bool Equals(object? obj)
    {
        return obj is Regex regex && Equals(regex);
    }
}
