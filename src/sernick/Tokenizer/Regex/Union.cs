namespace sernick.Tokenizer.Regex;

internal class Union : Regex
{
    public Union(IEnumerable<Regex> children)
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
