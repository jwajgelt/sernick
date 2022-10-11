namespace sernick.Tokenizer;

public class Token<TCat>
{
    public Token(TCat category, String text) {
        this.Category = category;
        this.Text = text;
    }
    public TCat Category { get; }
    public String Text { get; }
}
