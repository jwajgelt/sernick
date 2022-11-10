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
        var visitor = new NameResolvingAstVisitor();
        var result = visitor.VisitAstTree(ast, new NameResolutionVisitorParams());
        UsedVariableDeclarations = result.PartialResult.UsedVariableDeclarations;
        AssignedVariableDeclarations = result.PartialResult.AssignedVariableDeclarations;
        CalledFunctionDeclarations = result.PartialResult.CalledFunctionDeclarations;
    }

    
    private class NameResolvingAstVisitor : AstVisitor<NameResolutionVisitorResult, NameResolutionVisitorParams>
    {
        protected override NameResolutionVisitorResult VisitAstNode(AstNode node, NameResolutionVisitorParams param)
        {
            var results = node.Children.Select(child => child.Accept(this, param));
            return new NameResolutionVisitorResult(
                NameResolutionPartialResult.Join(results.Select(result => result.PartialResult)),
                    param.Variables
                    );
        }
    }
}
