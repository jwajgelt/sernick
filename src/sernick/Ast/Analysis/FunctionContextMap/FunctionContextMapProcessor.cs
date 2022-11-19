namespace sernick.Ast.Analysis.FunctionContextMap;

using System.Runtime.CompilerServices;
using Compiler.Function;
using NameResolution;
using Nodes;
using Utility;

/// <summary>
///     Static class with <see cref="Process"/> method, which constructs <see cref="IFunctionContext"/> for each
///     function declaration in a given AST. Wraps <see cref="FunctionContextProcessVisitor"/>
/// </summary>
public static class FunctionContextMapProcessor
{
    public static FunctionContextMap Process(AstNode ast, NameResolutionResult nameResolution, IFunctionFactory contextFactory)
    {
        var visitor = new FunctionContextProcessVisitor(nameResolution, contextFactory);
        visitor.VisitAstTree(ast, new FunctionContextVisitorParam());
        return visitor.ContextMap;
    }

    /// <summary>
    ///     Visitor class used to prepare FunctionContext for each function declaration from the AST.
    /// </summary>
    private sealed class FunctionContextProcessVisitor : AstVisitor<Unit, FunctionContextVisitorParam>
    {
        public readonly FunctionContextMap ContextMap = new();

        private readonly NameResolutionResult _nameResolution;
        private readonly IFunctionFactory _contextFactory;

        private readonly FunctionLocalVariables _locals = new();

        public FunctionContextProcessVisitor(NameResolutionResult nameResolution, IFunctionFactory contextFactory) =>
            (_nameResolution, _contextFactory) = (nameResolution, contextFactory);

        protected override Unit VisitAstNode(AstNode node, FunctionContextVisitorParam param)
        {
            foreach (var child in node.Children)
            {
                child.Accept(this, param);
            }

            return Unit.I;
        }

        public override Unit VisitFunctionDefinition(FunctionDefinition node, FunctionContextVisitorParam param)
        {
            var functionContext = _contextFactory.CreateFunction(
                parent: param.EnclosingFunction is not null ? ContextMap[param.EnclosingFunction] : null,
                parameters: node.Parameters.Select(parameter => new FunctionParamWrapper(parameter)).ToList(),
                returnsValue: !node.ReturnType.Equals(new UnitType()));

            ContextMap[node] = functionContext;

            _locals.EnterFunction(node);

            var newVisitorParam = new FunctionContextVisitorParam(EnclosingFunction: node);

            foreach (var parameter in node.Parameters)
            {
                parameter.Accept(this, newVisitorParam);
            }

            node.Body.Accept(this, newVisitorParam);

            // At this point, all local variables have been gathered
            foreach (var (localVariable, referencingFunctions) in _locals[node])
            {
                var usedElsewhere = referencingFunctions.Count() > 1;
                functionContext.AddLocal(new FunctionVariableWrapper(localVariable), usedElsewhere);
                _locals.DiscardLocal(localVariable);
            }

            _locals.ExitFunction(node);

            return Unit.I;
        }

        public override Unit VisitFunctionCall(FunctionCall node, FunctionContextVisitorParam param)
        {
            foreach (var argument in node.Arguments)
            {
                argument.Accept(this, param);
            }

            var functionDeclaration = _nameResolution.CalledFunctionDeclarations[node];
            ContextMap[node] = ContextMap[functionDeclaration];

            return Unit.I;
        }

        public override Unit VisitVariableDeclaration(VariableDeclaration node, FunctionContextVisitorParam param)
        {
            node.InitValue?.Accept(this, param);

            if (param.EnclosingFunction is not null)
            {
                _locals.DeclareLocal(node, param.EnclosingFunction);
            }

            return Unit.I;
        }

        public override Unit VisitFunctionParameterDeclaration(FunctionParameterDeclaration node, FunctionContextVisitorParam param)
        {
            if (param.EnclosingFunction is not null)
            {
                _locals.DeclareLocal(node, param.EnclosingFunction);
            }

            return Unit.I;
        }

        public override Unit VisitVariableValue(VariableValue node, FunctionContextVisitorParam param)
        {
            if (param.EnclosingFunction is not null)
            {
                var declaration = _nameResolution.UsedVariableDeclarations[node];
                _locals.UseLocal(declaration, param.EnclosingFunction);
            }

            return Unit.I;
        }

        public override Unit VisitAssignment(Assignment node, FunctionContextVisitorParam param)
        {
            node.Right.Accept(this, param);

            if (param.EnclosingFunction is not null)
            {
                var variableDeclaration = _nameResolution.AssignedVariableDeclarations[node];
                _locals.UseLocal(variableDeclaration, param.EnclosingFunction);
            }

            return Unit.I;
        }
    }
}

internal sealed record FunctionVariableWrapper(Declaration Variable) : FunctionVariable
{
    public bool Equals(FunctionVariableWrapper? other) => other is not null && ReferenceEquals(Variable, other.Variable);

    public override int GetHashCode() => RuntimeHelpers.GetHashCode(Variable);
}

internal sealed record FunctionParamWrapper(FunctionParameterDeclaration FunctionParameter) : FunctionParam
{
    public bool Equals(FunctionParamWrapper? other) =>
        other is not null && ReferenceEquals(FunctionParameter, other.FunctionParameter);

    public override int GetHashCode() => RuntimeHelpers.GetHashCode(FunctionParameter);
}
