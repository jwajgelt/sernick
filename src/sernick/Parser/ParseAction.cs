namespace sernick.Parser;

using sernick.Grammar.Syntax;

public interface IParseAction { };

public record ParseActionShift<TDfaState>(Configuration<TDfaState> Target) : IParseAction;

public record ParseActionReduce<TSymbol>(Production<TSymbol> Production) : IParseAction
    where TSymbol : IEquatable<TSymbol>;
