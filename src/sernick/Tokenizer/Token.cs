namespace sernick.Tokenizer;

using Input;
using Utility;

public sealed record Token<TCat>(TCat Category, string Text, Range<ILocation> LocationRange);
