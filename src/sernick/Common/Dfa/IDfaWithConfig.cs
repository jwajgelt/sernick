namespace sernick.Common.Dfa;

public interface IDfaWithConfig<TState> : IDfa<TState>
{
    IEnumerable<TransitionEdge> GetTransitionsFrom(TState state);
    IEnumerable<TransitionEdge> GetTransitionsTo(TState state);

    IEnumerable<TState> AcceptingStates { get; }

    public sealed record TransitionEdge(TState From, TState To, char Atom);
}
