using sernick.Input;

namespace sernick.Tokenizer;

public record Token<TCat>(TCat Category, string Text, ILocation Start, ILocation End);
