namespace sernick.Parser;

using Grammar.Syntax;

public interface IParseAction { }

public sealed record ParseActionShift<TSymbol>(Configuration<TSymbol> Target) : IParseAction where TSymbol : IEquatable<TSymbol>;

public sealed record ParseActionReduce<TSymbol>(Production<TSymbol> Production) : IParseAction
    where TSymbol : IEquatable<TSymbol>;
