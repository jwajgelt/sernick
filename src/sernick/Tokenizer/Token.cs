namespace sernick.Tokenizer;

public class Token<TCat>
{
    public Token(TCat category, String text) {
        Category = category;
        Text = text;
    }
    public TCat Category { get; }
    public String Text { get; }
}
