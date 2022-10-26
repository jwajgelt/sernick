namespace sernick.Parser;

public class Configuration<TDfaState>
{
    public Configuration(IReadOnlyCollection<TDfaState> states) => States = states;
    public IReadOnlyCollection<TDfaState> States { get; }

    public Configuration<TDfaState> Closure()
    {
        throw new NotImplementedException();
    }
}
