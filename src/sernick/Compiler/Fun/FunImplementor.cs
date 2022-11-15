namespace sernick.Compiler.Fun;

public abstract record FunVariable;

public abstract class FunImplementor : FunCaller
{
    public void AddLocal(FunVariable variable, bool usedElsewhere)
    {
        throw new NotImplementedException();
    }
}
