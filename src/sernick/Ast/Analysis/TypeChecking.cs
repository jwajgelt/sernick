namespace sernick.Ast.Analysis;

using Diagnostics;
using Nodes;
using Utility;

public class TypeChecking
{
    /// <summary>
    /// Maps expressions to their types
    /// </summary>
    public IReadOnlyDictionary<Expression, Type> ExpressionTypes
    {
        get;
        init;
    }

    public TypeChecking(AstNode ast, NameResolution nameResolution, Diagnostics diagnostics)
    {
        // TODO: implement a TypeCheckingASTVisitor, walk it over the AST and initialize the property with the result
        throw new NotImplementedException();
    }

    // TODO: use correct param and result types
    private class TypeCheckingAstVisitor : AstVisitor<Unit, Unit>
    {

    }
}
