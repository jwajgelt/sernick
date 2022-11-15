namespace sernick.Ast.Analysis.CallGraph;

using sernick.Ast.Nodes;
using sernick.Compiler.Fun;

/// <summary>
///     Static class with Process method, which constructs FunImplementors for each
///     function declaration in a given AST. Wraps FunPreprocessVisitor.
/// </summary>
public static class FunPreprocess
{
    public static IDictionary<FunctionDefinition, FunImplementor> Process(AstNode ast)
    {
        var visitor = new FunPreprocessVisitor();
        return visitor.VisitAstTree(ast, new FunPreprocessVisitorParam());
    }

    private sealed class FunPreprocessVisitorParam { }

    /// <summary>
    ///     Visitor class used to prepare FunImplementors for each function declaration from the AST.
    /// </summary>
    private sealed class FunPreprocessVisitor : AstVisitor<IDictionary<FunctionDefinition, FunImplementor>, FunPreprocessVisitorParam>
    {
        protected override IDictionary<FunctionDefinition, FunImplementor> VisitAstNode(AstNode node, FunPreprocessVisitorParam param)
        {
            throw new NotImplementedException();
        }
    }
}