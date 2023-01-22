namespace sernick.Ast.Analysis.FunctionContextMap;

using System.Diagnostics;
using Compiler.Function;
using NameResolution;
using Nodes;
using StructProperties;
using Utility;
using static ExternalFunctionsInfo;
using DistinctionNumberProvider = FunctionDistinctionNumberProcessor.DistinctionNumberProvider;

/// <summary>
///     Static class with <see cref="Process"/> method, which constructs <see cref="IFunctionContext"/> for each
///     function declaration in a given AST. Wraps <see cref="FunctionContextProcessVisitor"/>
/// </summary>
public static class FunctionContextMapProcessor
{
    public static FunctionContextMap Process(AstNode ast, NameResolutionResult nameResolution, StructProperties structProperties,
        DistinctionNumberProvider provider, IFunctionFactory contextFactory)
    {
        var visitor = new FunctionContextProcessVisitor(nameResolution, structProperties, provider, contextFactory);
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
        private readonly StructProperties _structProperties;
        private readonly DistinctionNumberProvider _provider;
        private readonly IFunctionFactory _contextFactory;

        private readonly FunctionLocalVariables _locals = new();

        public FunctionContextProcessVisitor(NameResolutionResult nameResolution, StructProperties structProperties, DistinctionNumberProvider provider, IFunctionFactory contextFactory) =>
            (_nameResolution, _structProperties, _provider, _contextFactory) = (nameResolution, structProperties, provider, contextFactory);

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
            int? retStructSize = null;
            if (node.ReturnType is StructType retType)
            {
                parameters = parameters.Prepend(new FunctionParameterDeclaration(
                    new Identifier("", node.LocationRange),
                    retType,
                    null,
                    node.LocationRange
                    )).ToList();
                retStructSize = _structProperties.StructSizes[retType.Struct];
            }

            var functionContext = _contextFactory.CreateFunction(
                parent: astContext.EnclosingFunction is not null ? ContextMap[astContext.EnclosingFunction] : null,
                name: node.Name,
                _provider(node),
                parameters: parameters,
                returnsValue: !node.ReturnType.Equals(new UnitType()),
                retStructSize);

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
                var varType = localVariable switch
                {
                    VariableDeclaration varDecl => varDecl.Type,
                    FunctionParameterDeclaration paramDecl => paramDecl.Type,
                    _ => throw new Exception("Local must be const/variable or function parameter."),
                };

                var usedElsewhere = referencingFunctions.Count() > 1;
                if (varType is StructType structType)
                {
                    functionContext.AddLocal(localVariable, usedElsewhere, true, _structProperties.StructSizes[structType.Struct]);
                }

                else
                {
                    functionContext.AddLocal(localVariable, usedElsewhere);
                }

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

            // "new" function is treated differently from others
            if (functionDeclaration.Name.Name == "new")
            {
                var argument = node.Arguments.First() as StructValue;
                if (argument == null) { return Unit.I; } // this should not happen

                var structDeclaration = _nameResolution.StructDeclarations[argument.StructName];
                var structSize = _structProperties.StructSizes[structDeclaration];
                ContextMap[node] = NewCallerFactory.GetMemcpyCaller(structSize);
            }
            else
            {
                ContextMap[node] = ExternalFunctions
                    .Where(external => ReferenceEquals(functionDeclaration, external.Definition))
                    .Select(external => external.Caller)
                    .FirstOrDefault()
                                   ?? ContextMap[functionDeclaration];
            }
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

            if (node.Left is not VariableValue value)
            {
                throw new NotImplementedException();
            }

            var variableDeclaration = _nameResolution.UsedVariableDeclarations[value];
            _locals.UseLocal(variableDeclaration, astContext.EnclosingFunction);

            return Unit.I;
        }
    }
}
