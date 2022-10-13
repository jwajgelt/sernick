namespace sernick.Tokenizer.Regex;

internal class AtomRegex : Regex
{
    public AtomRegex(char character)
    {
        Character = character;
    }
    public char Character { get; }

    public override bool ContainsEpsilon()
    {
        throw new NotImplementedException();
    }

    public override Regex Derivative(char atom)
    {
        throw new NotImplementedException();
    }

    public override int GetHashCode()
    {
        return Character.GetHashCode();
    }

    public override bool Equals(Regex? other)
    {
        return other is AtomRegex atomRegex && Character.Equals(atomRegex.Character);
    }
}
