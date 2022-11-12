namespace sernick.Ast.Analysis.NameResolution;

using Diagnostics;
using Nodes;

public sealed class NameResolution
{
    public NameResolution(AstNode ast, IDiagnostics diagnostics)
    {
        var visitor = new NameResolvingAstVisitor();
        var result = visitor.VisitAstTree(ast, new IdentifiersNamespace(diagnostics));
        (UsedVariableDeclarations, AssignedVariableDeclarations, CalledFunctionDeclarations) =
            result.PartialResult;
    }

    /// <summary>
    ///     Maps uses of variables to the declarations
    ///     of these variables
    /// </summary>
    public IReadOnlyDictionary<VariableValue, Declaration> UsedVariableDeclarations
    {
        get;
    }

    /// <summary>
    ///     Maps left-hand sides of assignments to variables
    ///     to the declarations of these variables.
    ///     NOTE: Since function parameters are non-assignable,
    ///     these can only be variable declarations (`var x`, `const y`)
    /// </summary>
    public IReadOnlyDictionary<Assignment, VariableDeclaration> AssignedVariableDeclarations
    {
        get;
    }

    /// <summary>
    ///     Maps AST nodes for function calls
    ///     to that function's declaration
    /// </summary>
    public IReadOnlyDictionary<FunctionCall, FunctionDefinition> CalledFunctionDeclarations
    {
        get;
    }

    private class NameResolvingAstVisitor : AstVisitor<NameResolutionVisitorResult, IdentifiersNamespace>
    {

        /// <summary>
        ///     A default implementation of Visit method, used to resolve names on the AST.
        /// </summary>
        /// <param name="node"> The node to call Visitor on. </param>
        /// <param name="identifiersNamespace"> An immutable class which holds information about currently visible identifiers. </param>
        /// <returns>
        ///     A VisitorResult, which is a pair of:
        ///     - a PartialNameResolutionResult containing the 3 result dictionaries filled with resolved names from the subtree,
        ///     - a new IdentifiersNamespace updated with identifiers defined inside of the subtree.
        /// </returns>
        protected override NameResolutionVisitorResult VisitAstNode(AstNode node, IdentifiersNamespace identifiersNamespace)
        {
            return node.Children.Aggregate(
                new NameResolutionVisitorResult(identifiersNamespace),
                (result, next) =>
                {
                    var childResult = next.Accept(this, result.IdentifiersNamespace);
                    return childResult with
                    {
                        PartialResult = PartialNameResolutionResult.Join(result.PartialResult, childResult.PartialResult)
                    };
                });
        }

        public override NameResolutionVisitorResult VisitVariableDeclaration(VariableDeclaration node,
            IdentifiersNamespace identifiersNamespace) => new(identifiersNamespace.Add(node));

        public override NameResolutionVisitorResult VisitFunctionParameterDeclaration(FunctionParameterDeclaration node,
            IdentifiersNamespace identifiersNamespace)
        {
            return new NameResolutionVisitorResult(new PartialNameResolutionResult(), identifiersNamespace.Add(node));
        }

        public override NameResolutionVisitorResult VisitFunctionDefinition(FunctionDefinition node,
            IdentifiersNamespace identifiersNamespace)
        {
            var variablesWithFunction = identifiersNamespace.Add(node);
            var variablesWithParameters = node.Parameters.Aggregate(variablesWithFunction.NewScope(),
                (variables, parameter) => parameter.Accept(this, variables).IdentifiersNamespace);

            var visitorResult = node.Body.Inner.Accept(this, variablesWithParameters);
            return visitorResult with { IdentifiersNamespace = variablesWithFunction };
        }

        public override NameResolutionVisitorResult VisitCodeBlock(CodeBlock node, IdentifiersNamespace identifiersNamespace)
        {
            var visitorResult = node.Inner.Accept(this, identifiersNamespace.NewScope());
            // ignore variables defined inside the block
            return visitorResult with { IdentifiersNamespace = identifiersNamespace };
        }

        public override NameResolutionVisitorResult VisitFunctionCall(FunctionCall node, IdentifiersNamespace identifiersNamespace)
        {
            var declaration = identifiersNamespace.GetCalledFunctionDeclaration(node.FunctionName);
            // if the identifier is not resolved, we can try to continue resolving and possibly find more errors
            return declaration == null
                ? new NameResolutionVisitorResult(identifiersNamespace)
                : new NameResolutionVisitorResult(PartialNameResolutionResult.OfFunctionCall(node, declaration),
                    identifiersNamespace);
        }

        public override NameResolutionVisitorResult VisitAssignment(Assignment node, IdentifiersNamespace identifiersNamespace)
        {
            var declaration = identifiersNamespace.GetAssignedVariableDeclaration(node.Left);
            // if the identifier is not resolved, we can try to continue resolving and possibly find more errors
            return declaration == null
                ? new NameResolutionVisitorResult(identifiersNamespace)
                : new NameResolutionVisitorResult(PartialNameResolutionResult.OfAssignment(node, declaration),
                    identifiersNamespace);
        }

        public override NameResolutionVisitorResult VisitVariableValue(VariableValue node, IdentifiersNamespace identifiersNamespace)
        {
            var declaration = identifiersNamespace.GetUsedVariableDeclaration(node.Identifier);
            // if the identifier is not resolved, we can try to continue resolving and possibly find more errors
            return declaration == null
                ? new NameResolutionVisitorResult(identifiersNamespace)
                : new NameResolutionVisitorResult(PartialNameResolutionResult.OfVariableUse(node, declaration),
                    identifiersNamespace);
        }
    }
}
