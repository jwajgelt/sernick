namespace sernick.Compiler.Function;
public static class NewCallerFactory
{
    public static MemcpyCaller GetMemcpyCaller(int structSize)
    {
        return new MemcpyCaller(structSize);
    }
}
