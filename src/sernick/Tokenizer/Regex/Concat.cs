namespace sernick.Tokenizer.Regex;

internal class Concat : Regex
{
    public Concat(IEnumerable<Regex> children)
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
