namespace sernick.Ast.Analysis;

using sernick.Compiler.Function;
public static class ExternalFunctionsInfo
{
    public record FunctionInfo(string Name, IFunctionCaller Caller);

    public static FunctionInfo[] ExternalFunctions = {
        new FunctionInfo("read", new ReadCaller()),
        new FunctionInfo("write", new WriteCaller()),
    };
}