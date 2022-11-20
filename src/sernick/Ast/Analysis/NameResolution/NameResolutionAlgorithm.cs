namespace sernick.Ast.Analysis.NameResolution;

using Diagnostics;
using Nodes;

public static class NameResolutionAlgorithm
{
    public static NameResolutionResult Process(AstNode ast, IDiagnostics diagnostics)
    {
        var visitor = new NameResolvingAstVisitor(diagnostics);
        return visitor.VisitAstTree(ast, new IdentifiersNamespace()).Result;
    }

    private sealed class NameResolvingAstVisitor : AstVisitor<NameResolutionVisitorResult, IdentifiersNamespace>
    {
        private readonly IDiagnostics _diagnostics;

        public NameResolvingAstVisitor(IDiagnostics diagnostics)
        {
            _diagnostics = diagnostics;
        }

        /// <summary>
        ///     A default implementation of Visit method, used to resolve names on the AST.
        /// </summary>
        /// <param name="node"> The node to call Visitor on. </param>
        /// <param name="identifiersNamespace"> An immutable class which holds information about currently visible identifiers. </param>
        /// <returns>
        ///     A VisitorResult, which is a pair of:
        ///     - a NameResolutionResult containing the 3 result dictionaries filled with resolved names from the subtree,
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
                        Result = result.Result.JoinWith(childResult.Result)
                    };
                });
        }

        public override NameResolutionVisitorResult VisitVariableDeclaration(VariableDeclaration node,
            IdentifiersNamespace identifiersNamespace)
        {
            try
            {
                var visitorResult = VisitAstNode(node, identifiersNamespace.Register(node));
                return visitorResult with { IdentifiersNamespace = visitorResult.IdentifiersNamespace.MakeVisible(node) };
            }
            catch (IdentifiersNamespace.IdentifierCollisionException)
            {
                _diagnostics.Report(new MultipleDeclarationsError(identifiersNamespace.GetDeclaredInThisScope(node.Name), node));
                return VisitAstNode(node, identifiersNamespace);
            }
            
        }
        public override NameResolutionVisitorResult VisitFunctionDefinition(FunctionDefinition node,
            IdentifiersNamespace identifiersNamespace)
        {
            var identifiersWithFunction = TryRegisterAndMakeVisible(identifiersNamespace, node);
            var identifiersWithParameters = node.Parameters.Aggregate(identifiersWithFunction.NewScope(),
                TryRegisterAndMakeVisible);

            var visitorResult = node.Body.Inner.Accept(this, identifiersWithParameters);
            return visitorResult with { IdentifiersNamespace = identifiersWithFunction };
        }

        public override NameResolutionVisitorResult VisitCodeBlock(CodeBlock node, IdentifiersNamespace identifiersNamespace)
        {
            var visitorResult = node.Inner.Accept(this, identifiersNamespace.NewScope());
            // ignore variables defined inside the block
            return visitorResult with { IdentifiersNamespace = identifiersNamespace };
        }

        public override NameResolutionVisitorResult VisitFunctionCall(FunctionCall node, IdentifiersNamespace identifiersNamespace)
        {
            var identifier = node.FunctionName;
            try
            {
                var declaration = identifiersNamespace.GetResolution(identifier);
                if (declaration is FunctionDefinition functionDefinition)
                {
                    var visitorResult = VisitAstNode(node, identifiersNamespace);
                    return visitorResult with
                    {
                        Result = visitorResult.Result.JoinWith(
                            NameResolutionResult.OfFunctionCall(node, functionDefinition))
                    };
                }

                _diagnostics.Report(new NotAFunctionError(node.FunctionName));
                return VisitAstNode(node, identifiersNamespace);
            }
            catch (IdentifiersNamespace.NoSuchIdentifierException)
            {
                _diagnostics.Report(new UndeclaredIdentifierError(identifier));
                return VisitAstNode(node, identifiersNamespace);
            }
        }

        public override NameResolutionVisitorResult VisitAssignment(Assignment node, IdentifiersNamespace identifiersNamespace)
        {
            var identifier = node.Left;
            try
            {
                var declaration = identifiersNamespace.GetResolution(identifier);
                if (declaration is VariableDeclaration variableDeclaration)
                {
                    var visitorResult = VisitAstNode(node, identifiersNamespace);
                    return visitorResult with
                    {
                        Result = visitorResult.Result.JoinWith(
                            NameResolutionResult.OfAssignment(node, variableDeclaration))
                    };
                }

                _diagnostics.Report(new NotAVariableError(node.Left));
                return VisitAstNode(node, identifiersNamespace);
            }
            catch (IdentifiersNamespace.NoSuchIdentifierException)
            {
                _diagnostics.Report(new UndeclaredIdentifierError(identifier));
                return VisitAstNode(node, identifiersNamespace);
            }
        }

        public override NameResolutionVisitorResult VisitVariableValue(VariableValue node, IdentifiersNamespace identifiersNamespace)
        {
            var identifier = node.Identifier;
            try
            {
                var declaration = identifiersNamespace.GetResolution(identifier);
                if (declaration is VariableDeclaration or FunctionParameterDeclaration)
                {
                    return new NameResolutionVisitorResult(NameResolutionResult.OfVariableUse(node, declaration),
                        identifiersNamespace);
                }

                _diagnostics.Report(new NotAVariableError(node.Identifier));
                return new NameResolutionVisitorResult(identifiersNamespace);
            }
            catch (IdentifiersNamespace.NoSuchIdentifierException)
            {
                _diagnostics.Report(new UndeclaredIdentifierError(identifier));
                return new NameResolutionVisitorResult(identifiersNamespace);
            }
        }

        /// <summary>
        /// Tries to add a new declaration to identifiers.
        /// If collision occurs, reports it to _diagnostics and returns the previous set of identifiers.
        /// </summary>
        private IdentifiersNamespace TryRegisterAndMakeVisible(IdentifiersNamespace identifiers, Declaration declaration)
        {
            try
            {
                return identifiers.RegisterAndMakeVisible(declaration);
            }
            catch (IdentifiersNamespace.IdentifierCollisionException)
            {
                _diagnostics.Report(new MultipleDeclarationsError(identifiers.GetDeclaredInThisScope(declaration.Name), declaration));
                return identifiers;
            }
        }
    }
}
