namespace sernick.Compiler.Fun;

public abstract record FunVariable;

public abstract class FunImplementor : FunCaller
{
    public abstract void AddLocal(FunVariable variable, bool usedElsewhere);
}
