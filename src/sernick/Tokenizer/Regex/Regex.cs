using System.Collections;

namespace sernick.Tokenizer.Regex;
public abstract class Regex : IEqualityComparer<Regex>
{
    public abstract bool ContainsEpsilon();
    public abstract Regex Derivative();

    public bool Equals(Regex? x, Regex? y)
    {
        throw new NotImplementedException();
    }

    public int GetHashCode(Regex obj)
    {
        throw new NotImplementedException();
    }
}