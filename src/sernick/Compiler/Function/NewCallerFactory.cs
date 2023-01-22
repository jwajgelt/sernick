namespace sernick.Compiler.Function;
public sealed class NewCallerFactory
{
    public static MemcpyCaller GetMemcpyCaller(int StructSize)
    {
        return new MemcpyCaller(StructSize);
    }
}
