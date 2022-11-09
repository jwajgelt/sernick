namespace sernick.Ast.Analysis;

using Diagnostics;
using Nodes;
using Utility;

public sealed class NameResolution
{
    /// <summary>
    /// Maps uses of variables to the declarations
    /// of these variables
    /// </summary>
    public IReadOnlyDictionary<VariableValue, Declaration> UsedVariableDeclarations
    {
        get;
        init;
    }

    /// <summary>
    /// Maps left-hand sides of assignments to variables
    /// to the declarations of these variables.
    /// NOTE: Since function parameters are non-assignable,
    /// these can only be variable declarations (`var x`, `const y`)
    /// </summary>
    public IReadOnlyDictionary<Assignment, VariableDeclaration> AssignedVariableDeclarations
    {
        get;
        init;
    }

    /// <summary>
    /// Maps AST nodes for function calls
    /// to that function's declaration
    /// </summary>
    public IReadOnlyDictionary<FunctionCall, FunctionDefinition> CalledFunctionDeclarations
    {
        get;
        init;
    }

    public NameResolution(AstNode ast, Diagnostics diagnostics)
    {
        // TODO: create a NameResolvingASTVisitor, walk it over the AST and initialize the properties with the result
        throw new NotImplementedException();
    }

    // TODO: use correct param and result types
    private class NameResolvingAstVisitor : AstVisitor<Unit, Unit>
    {
        protected override Unit VisitAstNode(AstNode node, Unit param)
        {
            foreach (var child in node.Children)
            {
                child.Accept(this, param);
            }

            return Unit.I;
        }
    }
}
