namespace sernick.Ast.Analysis.NameResolution;

using Diagnostics;
using Nodes;

public sealed class Algorithm
{
    public Algorithm(AstNode ast, IDiagnostics diagnostics)
    {
        var visitor = new NameResolvingAstVisitor();
        var result = visitor.VisitAstTree(ast, new LocalVariablesManager(diagnostics));
        UsedVariableDeclarations = result.PartialAlgorithmResult.UsedVariableDeclarations;
        AssignedVariableDeclarations = result.PartialAlgorithmResult.AssignedVariableDeclarations;
        CalledFunctionDeclarations = result.PartialAlgorithmResult.CalledFunctionDeclarations;
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

    private class NameResolvingAstVisitor : AstVisitor<VisitorResult, LocalVariablesManager>
    {

        /// <summary>
        ///     A default implementation of Visit method, used to resolve names on the AST.
        /// </summary>
        /// <param name="node"> The node to call Visitor on. </param>
        /// <param name="variablesManager"> An immutable manager which holds information about currently visible variables. </param>
        /// <returns>
        ///     A VisitorResult, which is a pair of:
        ///     - a PartialAlgorithmResult containing the 3 result dictionaries filled with resolved names from the subtree,
        ///     - a new LocalVariableManager updated with variables defined inside of the subtree.
        /// </returns>
        protected override VisitorResult VisitAstNode(AstNode node, LocalVariablesManager variablesManager)
        {
            var result = node.Children.Aggregate(
                (partialResult: new PartialAlgorithmResult(), variablesManager),
                (tuple, next) =>
                {
                    var childResult = next.Accept(this, tuple.variablesManager);
                    return (PartialAlgorithmResult.Join(tuple.partialResult, childResult.PartialAlgorithmResult),
                        variablesManager: childResult.VariablesManager);
                });

            return new VisitorResult(result.partialResult, result.variablesManager);
        }

        public override VisitorResult VisitVariableDeclaration(VariableDeclaration node,
            LocalVariablesManager variablesManager)
        {
            return new VisitorResult(new PartialAlgorithmResult(), variablesManager.Add(node));
        }

        public override VisitorResult VisitFunctionParameterDeclaration(FunctionParameterDeclaration node,
            LocalVariablesManager variablesManager)
        {
            return new VisitorResult(new PartialAlgorithmResult(), variablesManager.Add(node));
        }

        public override VisitorResult VisitFunctionDefinition(FunctionDefinition node,
            LocalVariablesManager variablesManager)
        {
            var variablesWithFunction = variablesManager.Add(node);
            var variablesWithParameters = node.Parameters.Aggregate(variablesWithFunction.NewScope(),
                (variables, parameter) => parameter.Accept(this, variables).VariablesManager);

            var visitorResult = node.Body.Inner.Accept(this, variablesWithParameters);
            return visitorResult with { VariablesManager = variablesWithFunction };
        }

        public override VisitorResult VisitCodeBlock(CodeBlock node, LocalVariablesManager variablesManager)
        {
            var visitorResult = node.Inner.Accept(this, variablesManager.NewScope());
            // ignore variables defined inside the block
            return visitorResult with { VariablesManager = variablesManager };
        }

        public override VisitorResult VisitFunctionCall(FunctionCall node, LocalVariablesManager variablesManager)
        {
            var declaration = variablesManager.GetCalledFunctionDeclaration(node.FunctionName);
            // if the identifier is not resolved, we can try to continue resolving and possibly find more errors
            return declaration == null
                ? new VisitorResult(variablesManager)
                : new VisitorResult(PartialAlgorithmResult.OfFunctionCall(node, declaration),
                    variablesManager);
        }

        public override VisitorResult VisitAssignment(Assignment node, LocalVariablesManager variablesManager)
        {
            var declaration = variablesManager.GetAssignedVariableDeclaration(node.Left);
            // if the identifier is not resolved, we can try to continue resolving and possibly find more errors
            return declaration == null
                ? new VisitorResult(variablesManager)
                : new VisitorResult(PartialAlgorithmResult.OfAssignment(node, declaration),
                    variablesManager);
        }

        public override VisitorResult VisitVariableValue(VariableValue node, LocalVariablesManager variablesManager)
        {
            var declaration = variablesManager.GetUsedVariableDeclaration(node.Identifier);
            // if the identifier is not resolved, we can try to continue resolving and possibly find more errors
            return declaration == null
                ? new VisitorResult(variablesManager)
                : new VisitorResult(PartialAlgorithmResult.OfVariableUse(node, declaration),
                    variablesManager);
        }
    }
}
