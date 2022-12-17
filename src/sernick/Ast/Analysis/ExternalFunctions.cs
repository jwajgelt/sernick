namespace sernick.Ast.Analysis;

using sernick.Compiler.Function;
using sernick.Ast.Nodes;
using sernick.Input;
using sernick.Input.String;
using sernick.Utility;

public static class ExternalFunctionsInfo
{
    public record FunctionInfo(FunctionDefinition Definition, IFunctionCaller Caller);

    private static Range<ILocation> placeholderRange = new Range<ILocation>(new StringLocation(0), new StringLocation(0));

    public static FunctionInfo[] ExternalFunctions = {
        new FunctionInfo(
            new FunctionDefinition(
                new Identifier("read", placeholderRange), 
                new List<FunctionParameterDeclaration>(), 
                new IntType(), 
                new CodeBlock(
                    new ReturnStatement(new IntLiteralValue(0, placeholderRange), placeholderRange), 
                    placeholderRange
                    ),
                placeholderRange
                ), 
                new ReadCaller()
        ),
        new FunctionInfo(
            new FunctionDefinition(
                new Identifier("write", placeholderRange), 
                new List<FunctionParameterDeclaration> { 
                    new FunctionParameterDeclaration(
                        new Identifier("placeholder", placeholderRange), 
                        new UnitType(), 
                        null, 
                        placeholderRange
                        ) 
                    }, 
                new IntType(), 
                new CodeBlock(
                    new ReturnStatement(null, placeholderRange), 
                    placeholderRange
                    ),
                placeholderRange
                ),
                new WriteCaller()
        ),
    };
}
