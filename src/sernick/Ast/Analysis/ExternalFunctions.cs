namespace sernick.Ast.Analysis;

using sernick.Compiler.Function;
using sernick.Ast.Nodes;
using sernick.Input.String;

public static class ExternalFunctionsInfo
{
    public record FunctionInfo(FunctionDefinition Definition, IFunctionCaller Caller);

    private static Range placeholderRange = new System.Range(new StringLocation(0), new StringLocation(0));

    public static FunctionInfo[] ExternalFunctions = {
        new FunctionInfo(
            new FunctionDefinition(
                new Identifier("read", placeholderRange), 
                new List<FunctionParameterDeclaration>(), 
                new IntType(), 
                new CodeBlock(new ReturnStatement(new IntLiteralValue(0, null), null), null),
                null
                ), 
                new ReadCaller()
        ),
        //new FunctionInfo("write", new WriteCaller()),
    };
}
