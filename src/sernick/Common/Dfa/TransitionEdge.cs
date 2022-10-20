namespace sernick.Common.Dfa;

public sealed record TransitionEdge<TState, TAtom>(TState From, TState To, TAtom Atom);
