using sernick.Input;

namespace sernick.Tokenizer;

public record Token<TCat>
{
    public TCat Category { get; init; }
    public String Text { get; init; }
    public ILocation Start { get; init; }
    public ILocation End { get; init; }
}
