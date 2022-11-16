namespace sernick.Ast.Analysis.FunctionMapPreprocess;

using sernick.Ast.Nodes;
using sernick.Compiler.Function;

/// <summary>
///     Static class with Process method, which constructs FunctionContext for each
///     function declaration in a given AST. Wraps FunctionPreprocessVisitor.
/// </summary>
public static class FunctionMapPreprocess
{
    public static IDictionary<FunctionDefinition, IFunctionContext> Process(AstNode ast)
    {
        var visitor = new FunctionPreprocessVisitor();
        return visitor.VisitAstTree(ast, new FunctionPreprocessVisitorParam());
    }

    private sealed class FunctionPreprocessVisitorParam { }

    /// <summary>
    ///     Visitor class used to prepare FunctionContext for each function declaration from the AST.
    /// </summary>
    private sealed class FunctionPreprocessVisitor : AstVisitor<IDictionary<FunctionDefinition, IFunctionContext>, FunctionPreprocessVisitorParam>
    {
        protected override IDictionary<FunctionDefinition, IFunctionContext> VisitAstNode(AstNode node, FunctionPreprocessVisitorParam param)
        {
            throw new NotImplementedException();
        }
    }
}
