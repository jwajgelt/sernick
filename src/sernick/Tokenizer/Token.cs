using sernick.Input;

namespace sernick.Tokenizer;

public class Token<TCat>
{
    public Token(TCat category, string text, ILocation start, ILocation end)
    {
        Category = category;
        Text = text;
        Start = start;
        End = end;
    }
    public TCat Category { get; }
    public String Text { get; }
    public ILocation Start { get; }
    public ILocation End { get; }
}
