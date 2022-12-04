namespace sernick.Ast.Analysis.TypeChecking;

using System.Linq;
using Diagnostics;
using NameResolution;
using Nodes;
using Utility;
using TypeInformation = Dictionary<Nodes.AstNode, Type>;

public static class TypeChecking
{
    /// <summary>
    /// Artificial type, which should not be used in a real programs
    /// But is convenient in type checking, when we do not care about
    /// what an expression returns. With "Any", we can specify it explicitly
    /// rather than doing some implicit checks on null/undefined/etc.
    /// </summary>
    private sealed record AnyType : Type
    {
        public override string ToString() => "Any";
    }

    public static TypeInformation CheckTypes(AstNode ast, NameResolutionResult nameResolution, IDiagnostics diagnostics)
    {
        var visitor = new TypeCheckingAstVisitor(nameResolution, diagnostics);
        return visitor.VisitAstTree(ast, new AnyType());
    }

    private class TypeCheckingAstVisitor : AstVisitor<TypeInformation, Type>
    {
        /// <summary>
        /// Invariant: when visiting AstNode X, types for all children of X are known
        /// and stored in partialExpressionTypes dictionary
        /// </summary>
        private readonly NameResolutionResult _nameResolution;
        private readonly IDiagnostics _diagnostics;
        /// <summary>
        /// Sometimes we would like to know the result for our ancestor,
        /// so to avoid recalculation (visiting the same ancestor from multiple nodes)
        /// we will have this helper object, containing type information for some AST nodes
        /// </summary>
        private readonly TypeInformation _partialResult;
        /// <summary>
        /// Our Type-checking Algorighm is a top-down postorder
        /// But sometimes, nodes need to know information about some other nodes higher up the tree
        /// For example, when we are visiting a variable value
        /// But variable declaration would be somewhere up the tree (and the type for it also)
        /// </summary>
        private readonly HashSet<AstNode> _pendingNodes;

        public TypeCheckingAstVisitor(NameResolutionResult nameResolution, IDiagnostics diagnostics)
        {
            _nameResolution = nameResolution;
            _diagnostics = diagnostics;
            _partialResult = new TypeInformation(ReferenceEqualityComparer.Instance);
            _pendingNodes = new HashSet<AstNode>();
        }

        protected override TypeInformation VisitAstNode(AstNode node, Type expectedReturnTypeOfReturnExpr)
        {
            _pendingNodes.Add(node);
            var result = node.Accept(this, expectedReturnTypeOfReturnExpr);
            _pendingNodes.Remove(node);
            return result;
        }

        public override TypeInformation VisitIdentifier(Identifier identifierNode, Type expectedReturnTypeOfReturnExpr)
        {
            _partialResult[identifierNode] = new UnitType();
            return new TypeInformation(ReferenceEqualityComparer.Instance) { { identifierNode, new UnitType() } };
        }

        public override TypeInformation VisitVariableDeclaration(VariableDeclaration node, Type expectedReturnTypeOfReturnExpr)
        {
            _pendingNodes.Add(node);
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);

            var declaredType = node.Type;
            if (declaredType != null && node.InitValue != null)
            {
                var rhsType = childrenTypes[node.InitValue];
                if (declaredType != rhsType)
                {
                    _diagnostics.Report(new TypeCheckingError(declaredType, rhsType, node.LocationRange.Start));
                }
            }

            var result = new TypeInformation(childrenTypes) { { node, new UnitType() } };
            _pendingNodes.Remove(node);
            return result;

        }

        public override TypeInformation VisitFunctionParameterDeclaration(FunctionParameterDeclaration node, Type expectedReturnTypeOfReturnExpr)
        {
            _pendingNodes.Add(node);
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);

            var result = new TypeInformation(childrenTypes) { { node, new UnitType() } };
            _pendingNodes.Remove(node);
            return result;
        }

        public override TypeInformation VisitFunctionDefinition(FunctionDefinition node, Type expectedReturnTypeOfReturnExpr)
        {
            _pendingNodes.Add(node);
            var declaredReturnType = node.ReturnType;
            var childrenTypes = VisitNodeChildren(node, declaredReturnType);
            var bodyReturnType = childrenTypes[node.Body];
            if (declaredReturnType != bodyReturnType)
            {
                _diagnostics.Report(new TypeCheckingError(declaredReturnType, bodyReturnType, node.LocationRange.Start));
            }

            var result = new TypeInformation(childrenTypes) { { node, new UnitType() } };
            _pendingNodes.Remove(node);
            return result;
        }

        public override TypeInformation VisitCodeBlock(CodeBlock node, Type expectedReturnTypeOfReturnExpr)
        {
            _pendingNodes.Add(node);
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);

            // simply return what expression inside returns?
            var result = new TypeInformation(childrenTypes) { { node, childrenTypes[node.Inner] } };
            _pendingNodes.Remove(node);
            return result;

        }

        public override TypeInformation VisitExpressionJoin(ExpressionJoin node, Type expectedReturnTypeOfReturnExpr)
        {
            _pendingNodes.Add(node);
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);

            // just return the last expressions' type
            var result = new TypeInformation(childrenTypes) { { node, childrenTypes[node.Second] } };
            _pendingNodes.Remove(node);
            return result;
        }

        public override TypeInformation VisitFunctionCall(FunctionCall functionCallNode, Type expectedReturnTypeOfReturnExpr)
        {
            _pendingNodes.Add(functionCallNode);
            var childrenTypes = VisitNodeChildren(functionCallNode, expectedReturnTypeOfReturnExpr);

            var functionDeclarationNode = _nameResolution.CalledFunctionDeclarations[functionCallNode];
            var declaredReturnType = functionDeclarationNode.ReturnType;

            var declaredArguments = functionDeclarationNode.Parameters;
            var actualArguments = functionCallNode.Arguments;

            if (declaredArguments.Count() != actualArguments.Count())
            {
                _diagnostics.Report(new FunctionArgumentsMismatchError(declaredArguments.Count(), actualArguments.Count(), functionCallNode.LocationRange.Start));
            }

            foreach (var (declaredArgument, actualArgument) in declaredArguments.Zip(actualArguments))
            {
                // let us do type checking right here
                var expectedType = declaredArgument.Type;
                var actualType = childrenTypes[actualArgument];
                if (expectedType != actualType)
                {
                    _diagnostics.Report(new WrongFunctionArgumentError(expectedType, actualType, functionCallNode.LocationRange.Start));
                }
            };

            var result = new TypeInformation(childrenTypes) { { functionCallNode, declaredReturnType } };
            _partialResult[functionCallNode] = declaredReturnType;
            _pendingNodes.Remove(functionCallNode);
            return result;
        }

        public override TypeInformation VisitContinueStatement(ContinueStatement node, Type expectedReturnTypeOfReturnExpr)
        {
            // Continue statement has no children, so we do not visit them
            var result = new TypeInformation(ReferenceEqualityComparer.Instance) { { node, new UnitType() } };
            return result;
        }

        public override TypeInformation VisitReturnStatement(ReturnStatement node, Type expectedReturnTypeOfReturnExpr)
        {
            _pendingNodes.Add(node);
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);

            var result = new TypeInformation(childrenTypes);
            // Return Value is in a subtree, so its type should be already calculated by now
            var returnValueType = (node.ReturnValue != null) ? childrenTypes[node.ReturnValue] : new UnitType();

            if (returnValueType == expectedReturnTypeOfReturnExpr || expectedReturnTypeOfReturnExpr is AnyType)
            {
                result.Add(node, returnValueType);
            }
            else
            {
                result.Add(node, new UnitType());
                _diagnostics.Report(new ReturnTypeError(expectedReturnTypeOfReturnExpr, returnValueType, node.LocationRange.Start));

            }

            _pendingNodes.Remove(node);
            return result;
        }

        public override TypeInformation VisitBreakStatement(BreakStatement node, Type expectedReturnTypeOfReturnExpr)
        {
            // Break statement should have no children
            _partialResult[node] = new UnitType();
            var result = new TypeInformation(ReferenceEqualityComparer.Instance) { { node, new UnitType() } };
            return result;
        }

        public override TypeInformation VisitIfStatement(IfStatement node, Type expectedReturnTypeOfReturnExpr)
        {
            _pendingNodes.Add(node);
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);

            var typeOfTrueBranch = childrenTypes[node.IfBlock];
            var typeOfCondition = childrenTypes[node.Condition];
            if (typeOfCondition is not BoolType)
            {
                _diagnostics.Report(new TypeCheckingError(new BoolType(), typeOfCondition, node.LocationRange.Start));
            }

            if (node.ElseBlock != null)
            {
                var typeOfFalseBranch = childrenTypes[node.ElseBlock];
                if (typeOfTrueBranch != typeOfFalseBranch)
                {
                    _diagnostics.Report(new UnequalBranchTypeError(typeOfTrueBranch, typeOfFalseBranch, node.LocationRange.Start));
                }
            }

            var result = new TypeInformation(childrenTypes) { { node, typeOfTrueBranch } };
            _partialResult[node] = typeOfTrueBranch;
            _pendingNodes.Remove(node);
            return result;
        }

        /// <summary>
        /// Loop always returns a `Unit` type
        /// break/return inside the loop would exit the loop
        /// and have no effect on the loop 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="_"></param>
        /// <returns></returns>
        public override TypeInformation VisitLoopStatement(LoopStatement node, Type expectedReturnTypeOfReturnExpr)
        {
            _pendingNodes.Add(node);
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);

            var result = new TypeInformation(childrenTypes) { { node, new UnitType() } };
            _pendingNodes.Remove(node);
            return result;
        }

        public override TypeInformation VisitEmptyExpression(EmptyExpression node, Type _)
        {
            _partialResult[node] = new UnitType();
            return new TypeInformation(ReferenceEqualityComparer.Instance) { { node, new UnitType() } };
        }

        public override TypeInformation VisitInfix(Infix node, Type expectedReturnTypeOfReturnExpr)
        {
            _pendingNodes.Add(node);
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);

            var typeOfLeftOperand = childrenTypes[node.Left];
            var typeOfRightOperand = childrenTypes[node.Right];

            if (typeOfLeftOperand is UnitType || typeOfRightOperand is UnitType)
            {
                _diagnostics.Report(new InfixOperatorTypeError(node.Operator, typeOfLeftOperand, typeOfRightOperand, node.LocationRange.Start));
                var result = new TypeInformation(childrenTypes) { { node, new UnitType() } };
                _pendingNodes.Remove(node);
                return result;
            }

            if (typeOfLeftOperand.ToString() != typeOfRightOperand.ToString())
            {
                _diagnostics.Report(new InfixOperatorTypeError(node.Operator, typeOfLeftOperand, typeOfRightOperand, node.LocationRange.Start));

                // TODO does it make sense to return anything here? maybe a Unit type? But it could propagate the error up the tree 
                var result = new TypeInformation(childrenTypes) { { node, new UnitType() } };
                _pendingNodes.Remove(node);
                return result;
            }
            else
            {
                var commonType = typeOfLeftOperand;

                // let's cover some special cases e.g. adding two bools or shirt-curcuiting two ints
                switch (node.Operator)
                {
                    case Infix.Op.ScAnd:
                    case Infix.Op.ScOr:
                        {
                            if (commonType is not BoolType)
                            {
                                _diagnostics.Report(new InfixOperatorTypeError(node.Operator, typeOfLeftOperand, typeOfRightOperand, node.LocationRange.Start));
                            }

                            break;
                        }
                    case Infix.Op.Plus:
                    case Infix.Op.Minus:
                    case Infix.Op.Greater:
                    case Infix.Op.GreaterOrEquals:
                    case Infix.Op.Less:
                    case Infix.Op.LessOrEquals:
                        {
                            if (commonType is not IntType)
                            {
                                _diagnostics.Report(new InfixOperatorTypeError(node.Operator, typeOfLeftOperand, typeOfRightOperand, node.LocationRange.Start));
                            }

                            break;
                        }
                    default:
                        {
                            break;
                        }
                }

                var result = new TypeInformation(childrenTypes) { { node, commonType } };
                _pendingNodes.Remove(node);
                return result;
            }
        }

        public override TypeInformation VisitAssignment(Assignment node, Type expectedReturnTypeOfReturnExpr)
        {
            _pendingNodes.Add(node);
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);

            var typeOfLeftSide = _partialResult[_nameResolution.AssignedVariableDeclarations[node]];
            var typeOfRightSide = childrenTypes[node.Right];
            if (typeOfLeftSide.ToString() != typeOfRightSide.ToString())
            {
                _diagnostics.Report(new TypeCheckingError(typeOfLeftSide, typeOfRightSide, node.LocationRange.Start));
            }

            // Regardless of the error, let's return a Unit type for assignment and get more type checking information
            var result = new TypeInformation(childrenTypes) { { node, typeOfLeftSide } };
            _pendingNodes.Remove(node);
            return result;
        }

        public override TypeInformation VisitVariableValue(VariableValue node, Type expectedReturnTypeOfReturnExpr)
        {
            var variableDeclarationNode = _nameResolution.UsedVariableDeclarations[node];
            var typeOfVariable = _partialResult[variableDeclarationNode];
            var result = new TypeInformation(ReferenceEqualityComparer.Instance) { { node, typeOfVariable } };
            return result;
        }

        public override TypeInformation VisitBoolLiteralValue(BoolLiteralValue node, Type expectedReturnTypeOfReturnExpr)
        {
            // No need to visit node children here

            var result = new TypeInformation(ReferenceEqualityComparer.Instance) { { node, new BoolType() } };
            return result;
        }

        public override TypeInformation VisitIntLiteralValue(IntLiteralValue node, Type expectedReturnTypeOfReturnExpr)
        {
            // No need to visit node children here

            var result = new TypeInformation(ReferenceEqualityComparer.Instance) { { node, new IntType() } };
            return result;
        }

        /// <summary>
        /// Since we want to do a bottom-up recursion, but we're calling our node.Accept functions
        /// in a top-down order, before actually processing a node we have to make sure we've visited all of its
        /// children. This helper method should be thus called at the beginning of almost each "Visit" function,
        /// except simple AST nodes (e.g. int literal), where we know there would be no recursion
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private TypeInformation VisitNodeChildren(AstNode node, Type expectedReturnTypeOfReturnExpr)
        {
            var avoidRecalculation = _partialResult.ContainsKey(node);
            if (avoidRecalculation)
            {
                return _partialResult;
            }

            return node.Children.Aggregate(new TypeInformation(ReferenceEqualityComparer.Instance),
                (partialTypeInformation, childNode) =>
                {
                    var resultForChildNode = childNode.Accept(this, expectedReturnTypeOfReturnExpr);
                    return new TypeInformation(
                        partialTypeInformation.JoinWith(resultForChildNode, ReferenceEqualityComparer.Instance));
                }
           );
        }
    }
}
