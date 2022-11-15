namespace sernick.Compiler.Fun;

public abstract record FunParam;

public sealed class FunFactory
{
    public FunImplementor MoreFun(FunImplementor? parent, IReadOnlyCollection<FunParam> parameters, bool result)
    {
        throw new NotImplementedException();
    }
}