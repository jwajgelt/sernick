namespace sernick.Tokenizer;

using Input;

public sealed record Token<TCat>(TCat Category, string Text, ILocation Start, ILocation End);
