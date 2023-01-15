namespace sernick.Ast.Analysis.FunctionContextMap;

using System.Diagnostics;
using Compiler.Function;
using NameResolution;
using Nodes;
using Utility;
using static ExternalFunctionsInfo;
using DistinctionNumberProvider = FunctionDistinctionNumberProcessor.DistinctionNumberProvider;

/// <summary>
///     Static class with <see cref="Process"/> method, which constructs <see cref="IFunctionContext"/> for each
///     function declaration in a given AST. Wraps <see cref="FunctionContextProcessVisitor"/>
/// </summary>
public static class FunctionContextMapProcessor
{
    public static FunctionContextMap Process(AstNode ast, NameResolutionResult nameResolution,
        DistinctionNumberProvider provider, IFunctionFactory contextFactory)
    {
        var visitor = new FunctionContextProcessVisitor(nameResolution, provider, contextFactory);
        visitor.VisitAstTree(ast, new AstNodeContext());
        return visitor.ContextMap;
    }

    /// <summary>
    /// Only invisible "main()" declaration will be visited with <c>EnclosingFunction == null</c>
    /// </summary>
    private record struct AstNodeContext(FunctionDefinition? EnclosingFunction = null);

    /// <summary>
    ///     Visitor class used to prepare FunctionContext for each function declaration from the AST.
    /// </summary>
    private sealed class FunctionContextProcessVisitor : AstVisitor<Unit, AstNodeContext>
    {
        public readonly FunctionContextMap ContextMap = new();

        private readonly NameResolutionResult _nameResolution;
        private readonly DistinctionNumberProvider _provider;
        private readonly IFunctionFactory _contextFactory;

        private readonly FunctionLocalVariables _locals = new();

        public FunctionContextProcessVisitor(NameResolutionResult nameResolution, DistinctionNumberProvider provider, IFunctionFactory contextFactory) =>
            (_nameResolution, _provider, _contextFactory) = (nameResolution, provider, contextFactory);

        protected override Unit VisitAstNode(AstNode node, AstNodeContext param)
        {
            foreach (var child in node.Children)
            {
                child.Accept(this, param);
            }

            return Unit.I;
        }

        public override Unit VisitFunctionDefinition(FunctionDefinition node, AstNodeContext astContext)
        {
            var parameters = node.Parameters.ToList();
            if (node.ReturnType is StructType type)
            {
                parameters.Prepend(new FunctionParameterDeclaration(
                    new Identifier("", node.LocationRange),
                    type,
                    null,
                    node.LocationRange
                    ));
            }

            var functionContext = _contextFactory.CreateFunction(
                parent: astContext.EnclosingFunction is not null ? ContextMap[astContext.EnclosingFunction] : null,
                name: node.Name,
                _provider(node),
                parameters: node.Parameters.ToList(),
                returnsValue: !node.ReturnType.Equals(new UnitType()),
                node.ReturnType is StructType);

            ContextMap[node] = functionContext;

            _locals.EnterFunction(node);

            var newAstContext = new AstNodeContext(EnclosingFunction: node);

            foreach (var parameter in node.Parameters)
            {
                parameter.Accept(this, newAstContext);
            }

            node.Body.Accept(this, newAstContext);

            // At this point, all local variables have been gathered
            foreach (var (localVariable, referencingFunctions) in _locals[node])
            {
                var usedElsewhere = referencingFunctions.Count() > 1;
                functionContext.AddLocal(localVariable, usedElsewhere);
                _locals.DiscardLocal(localVariable);
            }

            _locals.ExitFunction(node);

            return Unit.I;
        }

        public override Unit VisitFunctionCall(FunctionCall node, AstNodeContext astContext)
        {
            foreach (var argument in node.Arguments)
            {
                argument.Accept(this, astContext);
            }

            var functionDeclaration = _nameResolution.CalledFunctionDeclarations[node];
            ContextMap[node] = ExternalFunctions
                .Where(external => ReferenceEquals(functionDeclaration, external.Definition))
                .Select(external => external.Caller)
                .FirstOrDefault()
                               ?? ContextMap[functionDeclaration];

            return Unit.I;
        }

        public override Unit VisitVariableDeclaration(VariableDeclaration node, AstNodeContext astContext)
        {
            Debug.Assert(astContext.EnclosingFunction is not null);

            node.InitValue?.Accept(this, astContext);

            _locals.DeclareLocal(node, astContext.EnclosingFunction);

            return Unit.I;
        }

        public override Unit VisitFunctionParameterDeclaration(FunctionParameterDeclaration node, AstNodeContext astContext)
        {
            Debug.Assert(astContext.EnclosingFunction is not null);

            _locals.DeclareLocal(node, astContext.EnclosingFunction);

            return Unit.I;
        }

        public override Unit VisitVariableValue(VariableValue node, AstNodeContext astContext)
        {
            Debug.Assert(astContext.EnclosingFunction is not null);

            var declaration = _nameResolution.UsedVariableDeclarations[node];
            _locals.UseLocal(declaration, astContext.EnclosingFunction);

            return Unit.I;
        }

        public override Unit VisitAssignment(Assignment node, AstNodeContext astContext)
        {
            Debug.Assert(astContext.EnclosingFunction is not null);

            node.Right.Accept(this, astContext);

            var variableDeclaration = _nameResolution.AssignedVariableDeclarations[node];
            _locals.UseLocal(variableDeclaration, astContext.EnclosingFunction);

            return Unit.I;
        }
    }
}
