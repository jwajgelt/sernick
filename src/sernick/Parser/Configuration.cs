namespace sernick.Parser;

public sealed record Configuration<TDfaState>(
    IReadOnlyCollection<TDfaState> States
)
{
    public Configuration<TDfaState> Closure()
    {
        throw new NotImplementedException();
    }
}
