namespace sernick.Tokenizer.Regex;

internal sealed class AtomRegex : Regex
{
    public AtomRegex(char character)
    {
        Character = character;
    }
    public char Character { get; }

    public override bool ContainsEpsilon() => false;

    public override Regex Derivative(char atom)
    {
        return Character == atom ? Epsilon : Empty;
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
    public static partial Regex Atom(char character) => new AtomRegex(character);
}
