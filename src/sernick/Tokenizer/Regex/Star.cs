namespace sernick.Tokenizer.Regex;

internal class Star : Regex
{
    public Star(Regex child)
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