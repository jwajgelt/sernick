namespace sernick.Tokenizer.Regex;

internal class Atom : Regex
{
    public Atom(char character)
    {

    }

    public override bool ContainsEpsilon()
    {
        throw new NotImplementedException();
    }

    public override Regex Derivative()
    {
        throw new NotImplementedException();
    }
}
