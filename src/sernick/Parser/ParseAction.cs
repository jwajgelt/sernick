namespace sernick.Parser;

using Grammar.Syntax;

public interface IParseAction { }

public sealed record ParseActionShift<TDfaState>(Configuration<TDfaState> Target) : IParseAction;

public sealed record ParseActionReduce<TSymbol>(Production<TSymbol> Production) : IParseAction
    where TSymbol : IEquatable<TSymbol>;
