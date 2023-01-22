namespace sernick.Compiler.Function;
public sealed class NewCallerFactory
{
    public static MemcpyCaller GetMemcpyCaller(int structSize)
    {
        return new MemcpyCaller(structSize);
    }
}
