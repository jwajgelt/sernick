namespace sernick.Ast.Analysis.NameResolution;

using Diagnostics;
using Nodes;
using Utility;
using static ExternalFunctionsInfo;

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
            return VisitConsecutiveNodes(node.Children, identifiersNamespace);
        }

        public override NameResolutionVisitorResult VisitVariableDeclaration(VariableDeclaration node,
            IdentifiersNamespace identifiersNamespace)
        {
            if (node.InitValue is null)
            {
                return new NameResolutionVisitorResult(TryAdd(identifiersNamespace, node));
            }

            var visitorResult = node.InitValue.Accept(this, identifiersNamespace);
            var updatedIdentifiers = TryAdd(visitorResult.IdentifiersNamespace, node);
            return visitorResult with { IdentifiersNamespace = updatedIdentifiers };
        }
        
        public override NameResolutionVisitorResult VisitFunctionDefinition(FunctionDefinition node,
            IdentifiersNamespace identifiersNamespace)
        {
            // Parameters, Body and ReturnType contribute to the name resolution result
            var identifiersWithFunction = TryAdd(identifiersNamespace, node);
        
            var toVisit = new List<AstNode>(node.Parameters) { node.Body.Inner };

            var visitResult = VisitConsecutiveNodes(toVisit, identifiersWithFunction.NewScope());
            var returnTypeStructs = FindStructDeclarationsInType(identifiersNamespace, node.ReturnType);
            var nameResolutionResult = visitResult.Result.AddStructs(returnTypeStructs);
            return new NameResolutionVisitorResult(nameResolutionResult, identifiersWithFunction);
        }

        public override NameResolutionVisitorResult VisitFunctionParameterDeclaration(FunctionParameterDeclaration node,
            IdentifiersNamespace identifiersNamespace)
        {
            var structNames = FindStructDeclarationsInType(identifiersNamespace, node.Type);
            var identifiers = TryAdd(identifiersNamespace, node);
            var result = NameResolutionResult.OfStructs(structNames);
            return new(result, identifiers);
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
                var declaration = ExternalFunctions
                    .Select(external => external.Definition)
                    .FirstOrDefault(definition => identifier.Name.Equals(definition.Name.Name))
                                  ?? identifiersNamespace.GetResolution(identifier);
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
            if (node.Left is not VariableValue { Identifier: var identifier })
            {
                throw new NotImplementedException();
            }

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

                _diagnostics.Report(new NotAVariableError(identifier));
                return VisitAstNode(node, identifiersNamespace);
            }
            catch (IdentifiersNamespace.NoSuchIdentifierException)
            {
                _diagnostics.Report(new UndeclaredIdentifierError(identifier));
                return VisitAstNode(node, identifiersNamespace);
            }
        }

        public override NameResolutionVisitorResult VisitVariableValue(VariableValue node,
            IdentifiersNamespace identifiersNamespace)
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

        public override NameResolutionVisitorResult VisitStructDeclaration(StructDeclaration node, IdentifiersNamespace identifiersNamespace)
        {
            var updatedIdentifiers = TryAdd(identifiersNamespace, node);
            return VisitAstNode(node, updatedIdentifiers);
        }

        public override NameResolutionVisitorResult VisitFieldDeclaration(FieldDeclaration node,
            IdentifiersNamespace identifiersNamespace)
        {
            var structNames = FindStructDeclarationsInType(identifiersNamespace, node.Type);
            var result = NameResolutionResult.OfStructs(structNames);
            return new(result, identifiersNamespace);
        }

        public override NameResolutionVisitorResult VisitStructValue(StructValue node,
            IdentifiersNamespace identifiersNamespace)
        {
            var structNames = FindStructDeclaration(identifiersNamespace, node.StructName);
            var result = NameResolutionResult.OfStructs(structNames);
            return new(result, identifiersNamespace);
        }
        
        private NameResolutionVisitorResult VisitConsecutiveNodes(IEnumerable<AstNode> nodes, IdentifiersNamespace identifiersNamespace)
        {
            return nodes.Aggregate(
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

        /// <summary>
        /// Tries to add a new declaration to identifiers.
        /// If collision occurs, reports it to _diagnostics and returns the previous set of identifiers.
        /// </summary>
        private IdentifiersNamespace TryAdd(IdentifiersNamespace identifiers, Declaration declaration)
        {
            try
            {
                return identifiers.Add(declaration);
            }
            catch (IdentifiersNamespace.IdentifierCollisionException)
            {
                _diagnostics.Report(new MultipleDeclarationsError(identifiers.GetResolution(declaration.Name), declaration));
                return identifiers;
            }
        }

        /// <summary>
        /// Given a Type, finds all struct references in it and returns their declarations
        /// a visitor for this would be an overkill
        /// </summary>
        private Dictionary<Identifier, StructDeclaration> FindStructDeclarationsInType(IdentifiersNamespace identifiersNamespace, Type type)
        {
            switch (type)
            {
                case StructType structType:
                    var identifier = structType.Struct;
                    return FindStructDeclaration(identifiersNamespace, identifier);
                case PointerType pointerType:
                    return FindStructDeclarationsInType(identifiersNamespace, pointerType.Type);
                default:
                    return new Dictionary<Identifier, StructDeclaration>();
            }
        }

        private Dictionary<Identifier, StructDeclaration> FindStructDeclaration(
            IdentifiersNamespace identifiersNamespace, Identifier identifier)
        {
            try
            {
                var declaration = identifiersNamespace.GetResolution(identifier);
                if (declaration is StructDeclaration structDeclaration)
                {
                    return new Dictionary<Identifier, StructDeclaration>
                    {
                        { identifier, structDeclaration }
                    };
                }
                _diagnostics.Report(new NotATypeError(identifier));
            }
            catch (IdentifiersNamespace.NoSuchIdentifierException)
            {
                _diagnostics.Report(new UndeclaredIdentifierError(identifier));
            }

            return new Dictionary<Identifier, StructDeclaration>();
        }
    }
}
