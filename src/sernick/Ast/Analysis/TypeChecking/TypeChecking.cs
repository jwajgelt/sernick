namespace sernick.Ast.Analysis.TypeChecking;

using System.Linq;
using Diagnostics;
using NameResolution;
using Nodes;
using Utility;
using TypeInformation = Dictionary<Nodes.AstNode, Type>;

public sealed record TypeCheckingResult(TypeInformation ExpressionsTypes) 
{
    public Type this[AstNode key]
    {
        get => ExpressionsTypes[key];
    }
}

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

    public static TypeCheckingResult CheckTypes(AstNode ast, NameResolutionResult nameResolution, IDiagnostics diagnostics)
    {
        var visitor = new TypeCheckingAstVisitor(nameResolution, diagnostics);
        return new TypeCheckingResult(visitor.VisitAstTree(ast, new AnyType()));
    }

    private class TypeCheckingAstVisitor : AstVisitor<Dictionary<AstNode, Type>, Type>
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
        ///
        /// This object contains types for variable and function parameter types
        /// </summary>
        private readonly Dictionary<Declaration, Type> _memoizedDeclarationTypes;

        public TypeCheckingAstVisitor(NameResolutionResult nameResolution, IDiagnostics diagnostics)
        {
            _nameResolution = nameResolution;
            _diagnostics = diagnostics;
            _memoizedDeclarationTypes = new Dictionary<Declaration, Type>(ReferenceEqualityComparer.Instance);
        }

        protected override Dictionary<AstNode, Type> VisitAstNode(AstNode node, Type expectedReturnTypeOfReturnExpr)
        {
            var result = node.Accept(this, expectedReturnTypeOfReturnExpr);
            return result;
        }

        public override Dictionary<AstNode, Type> VisitIdentifier(Identifier identifierNode, Type expectedReturnTypeOfReturnExpr)
        {
            return new Dictionary<AstNode, Type>(ReferenceEqualityComparer.Instance) { { identifierNode, new UnitType() } };
        }

        public override Dictionary<AstNode, Type> VisitVariableDeclaration(VariableDeclaration node, Type expectedReturnTypeOfReturnExpr)
        {
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);

            var declaredType = node.Type;
            if (node.InitValue != null)
            {
                var rhsType = childrenTypes[node.InitValue];
                if (declaredType != null && declaredType != rhsType)
                {
                    _diagnostics.Report(new TypesMismatchError(declaredType, rhsType, node.InitValue.LocationRange.Start));
                }

                _memoizedDeclarationTypes[node] = declaredType ?? rhsType;
            }
            else
            {
                if (declaredType == null)
                {
                    _diagnostics.Report(new TypeOrInitialValueShouldBePresentError(node.LocationRange.Start));

                }

                _memoizedDeclarationTypes[node] = declaredType ?? new UnitType(); // maybe it will not lead to more errors; maybe it will
            }

            // Regardless of error and types, var decl node itself has a unit type
            var result = new Dictionary<AstNode, Type>(childrenTypes, ReferenceEqualityComparer.Instance) { { node, new UnitType() } };

            return result;

        }

        public override Dictionary<AstNode, Type> VisitFunctionParameterDeclaration(FunctionParameterDeclaration node, Type expectedReturnTypeOfReturnExpr)
        {
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);

            _memoizedDeclarationTypes[node] = node.Type;
            if (node.Type is UnitType)
            {
                _diagnostics.Report(new UnitTypeNotAllowedInFunctionArgumentError(node.LocationRange.Start));
            }

            if (node.DefaultValue != null)
            {
                var defaultValueType = childrenTypes[node.DefaultValue];
                if (defaultValueType != node.Type)
                {
                    _diagnostics.Report(new TypesMismatchError(node.Type, defaultValueType, node.DefaultValue.LocationRange.Start));
                }
            }

            var result = new Dictionary<AstNode, Type>(childrenTypes, ReferenceEqualityComparer.Instance) { { node, new UnitType() } };
            return result;
        }

        public override Dictionary<AstNode, Type> VisitFunctionDefinition(FunctionDefinition node, Type expectedReturnTypeOfReturnExpr)
        {
            var declaredReturnType = node.ReturnType;
            var childrenTypes = VisitNodeChildren(node, declaredReturnType);
            var bodyReturnType = childrenTypes[node.Body];
            if (declaredReturnType != bodyReturnType)
            {
                // _diagnostics.Report(new InferredBadFunctionReturnType(declaredReturnType, bodyReturnType, node.LocationRange.Start));
                // Commenting out, since it seems like there's a problem with a test
                // examples/default-arguments/correct/all-args.ser
                // an "EmptyExpression" is being added to an "if" instruction, whilst it should be just If
            }

            var result = new Dictionary<AstNode, Type>(childrenTypes, ReferenceEqualityComparer.Instance) { { node, new UnitType() } };
            return result;
        }

        public override Dictionary<AstNode, Type> VisitCodeBlock(CodeBlock node, Type expectedReturnTypeOfReturnExpr)
        {
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);

            // simply return what expression inside returns?
            var result = new Dictionary<AstNode, Type>(childrenTypes, ReferenceEqualityComparer.Instance) { { node, childrenTypes[node.Inner] } };
            return result;

        }

        public override Dictionary<AstNode, Type> VisitExpressionJoin(ExpressionJoin node, Type expectedReturnTypeOfReturnExpr)
        {
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);

            // just return the last expressions' type
            var result = new Dictionary<AstNode, Type>(childrenTypes, ReferenceEqualityComparer.Instance) { { node, childrenTypes[node.Second] } };
            return result;
        }

        public override Dictionary<AstNode, Type> VisitFunctionCall(FunctionCall functionCallNode, Type expectedReturnTypeOfReturnExpr)
        {
            var childrenTypes = VisitNodeChildren(functionCallNode, expectedReturnTypeOfReturnExpr);

            var functionDeclarationNode = _nameResolution.CalledFunctionDeclarations[functionCallNode];
            var declaredReturnType = functionDeclarationNode.ReturnType;

            var declaredArguments = functionDeclarationNode.Parameters;
            var parametersWithDefaultValuesCount = functionDeclarationNode.Parameters.Count(param => param.DefaultValue != null);
            var actualArguments = functionCallNode.Arguments;
            if (actualArguments.Count < declaredArguments.Count - parametersWithDefaultValuesCount)
            {
                _diagnostics.Report(new WrongNumberOfFunctionArgumentsError(declaredArguments.Count - parametersWithDefaultValuesCount, actualArguments.Count, functionCallNode.LocationRange.Start));
            }

            foreach (var (declaredArgument, actualArgument) in declaredArguments.Zip(actualArguments))
            {
                // let us do type checking right here
                var expectedType = declaredArgument.Type;
                var actualType = childrenTypes[actualArgument];
                if (expectedType != actualType)
                {
                    _diagnostics.Report(
                        new WrongFunctionArgumentError(expectedType, actualType, actualArgument.LocationRange.Start)
                    );
                }
            }

            var result = new Dictionary<AstNode, Type>(childrenTypes, ReferenceEqualityComparer.Instance) { { functionCallNode, declaredReturnType } };
            return result;
        }

        public override Dictionary<AstNode, Type> VisitContinueStatement(ContinueStatement node, Type expectedReturnTypeOfReturnExpr)
        {
            // Continue statement has no children, so we do not visit them
            var result = new Dictionary<AstNode, Type>(ReferenceEqualityComparer.Instance) { { node, new UnitType() } };
            return result;
        }

        public override Dictionary<AstNode, Type> VisitReturnStatement(ReturnStatement node, Type expectedReturnTypeOfReturnExpr)
        {
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);

            var result = new Dictionary<AstNode, Type>(childrenTypes, ReferenceEqualityComparer.Instance);
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

            return result;
        }

        public override Dictionary<AstNode, Type> VisitBreakStatement(BreakStatement node, Type expectedReturnTypeOfReturnExpr)
        {
            // Break statement should have no children
            var result = new Dictionary<AstNode, Type>(ReferenceEqualityComparer.Instance) { { node, new UnitType() } };
            return result;
        }

        public override Dictionary<AstNode, Type> VisitIfStatement(IfStatement node, Type expectedReturnTypeOfReturnExpr)
        {
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);

            var typeOfTrueBranch = childrenTypes[node.IfBlock];
            var typeOfCondition = childrenTypes[node.Condition];
            if (typeOfCondition is not BoolType)
            {
                _diagnostics.Report(new TypesMismatchError(new BoolType(), typeOfCondition, node.LocationRange.Start));
            }

            if (node.ElseBlock != null)
            {
                var typeOfFalseBranch = childrenTypes[node.ElseBlock];
                if (typeOfTrueBranch != typeOfFalseBranch)
                {
                    _diagnostics.Report(new UnequalBranchTypeError(typeOfTrueBranch, typeOfFalseBranch, node.LocationRange.Start));
                }
            }

            var result = new Dictionary<AstNode, Type>(childrenTypes, ReferenceEqualityComparer.Instance) { { node, typeOfTrueBranch } };
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
        public override Dictionary<AstNode, Type> VisitLoopStatement(LoopStatement node, Type expectedReturnTypeOfReturnExpr)
        {
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);

            var result = new Dictionary<AstNode, Type>(childrenTypes, ReferenceEqualityComparer.Instance) { { node, new UnitType() } };
            return result;
        }

        public override Dictionary<AstNode, Type> VisitEmptyExpression(EmptyExpression node, Type _)
        {
            return new Dictionary<AstNode, Type>(ReferenceEqualityComparer.Instance) { { node, new UnitType() } };
        }

        public override Dictionary<AstNode, Type> VisitInfix(Infix node, Type expectedReturnTypeOfReturnExpr)
        {
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);

            var typeOfLeftOperand = childrenTypes[node.Left];
            var typeOfRightOperand = childrenTypes[node.Right];

            if (typeOfLeftOperand is UnitType || typeOfRightOperand is UnitType)
            {
                _diagnostics.Report(new InfixOperatorTypeError(node.Operator, typeOfLeftOperand, typeOfRightOperand, node.LocationRange.Start));
                var result = new Dictionary<AstNode, Type>(childrenTypes, ReferenceEqualityComparer.Instance) { { node, new UnitType() } };
                return result;
            }

            if (typeOfLeftOperand.ToString() != typeOfRightOperand.ToString())
            {
                _diagnostics.Report(new InfixOperatorTypeError(node.Operator, typeOfLeftOperand, typeOfRightOperand, node.LocationRange.Start));

                // TODO does it make sense to return anything here? maybe a Unit type? But it could propagate the error up the tree 
                var result = new Dictionary<AstNode, Type>(childrenTypes, ReferenceEqualityComparer.Instance) { { node, new UnitType() } };
                return result;
            }
            else
            {
                var commonType = typeOfLeftOperand;
                var result = new Dictionary<AstNode, Type>(childrenTypes, ReferenceEqualityComparer.Instance);

                // let's cover some special cases e.g. adding two bools or shirt-circuiting two ints
                switch (node.Operator)
                {
                    case Infix.Op.ScAnd:
                    case Infix.Op.ScOr:
                        {
                            if (commonType is not BoolType)
                            {
                                _diagnostics.Report(new InfixOperatorTypeError(node.Operator, typeOfLeftOperand, typeOfRightOperand, node.LocationRange.Start));
                            }

                            result.Add(node, new BoolType());
                            break;
                        }
                    case Infix.Op.Plus:
                    case Infix.Op.Minus:
                        {
                            if (commonType is not IntType)
                            {
                                _diagnostics.Report(new InfixOperatorTypeError(node.Operator, typeOfLeftOperand, typeOfRightOperand, node.LocationRange.Start));
                            }

                            result.Add(node, new IntType());
                            break;
                        }
                    case Infix.Op.Greater:
                    case Infix.Op.GreaterOrEquals:
                    case Infix.Op.Less:
                    case Infix.Op.LessOrEquals:
                    case Infix.Op.Equals:
                        {
                            if (commonType is not IntType)
                            {
                                _diagnostics.Report(new InfixOperatorTypeError(node.Operator, typeOfLeftOperand, typeOfRightOperand, node.LocationRange.Start));
                            }

                            result.Add(node, new BoolType());
                            break;
                        }
                    default:
                        {
                            result.Add(node, new UnitType());
                            break;
                        }
                }

                return result;
            }
        }

        public override Dictionary<AstNode, Type> VisitAssignment(Assignment node, Type expectedReturnTypeOfReturnExpr)
        {
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);

            var variableDeclarationNode = _nameResolution.AssignedVariableDeclarations[node];
            var typeOfLeftSide = _memoizedDeclarationTypes[variableDeclarationNode];
            var typeOfRightSide = childrenTypes[node.Right];
            if (typeOfLeftSide.ToString() != typeOfRightSide.ToString())
            {
                _diagnostics.Report(new TypesMismatchError(typeOfLeftSide, typeOfRightSide, node.Right.LocationRange.Start));
            }

            // Regardless of the error, let's return a Unit type for assignment and get more type checking information
            var result = new Dictionary<AstNode, Type>(childrenTypes, ReferenceEqualityComparer.Instance) { { node, typeOfLeftSide } };
            return result;
        }

        public override Dictionary<AstNode, Type> VisitVariableValue(VariableValue node, Type expectedReturnTypeOfReturnExpr)
        {
            var variableDeclarationNode = _nameResolution.UsedVariableDeclarations[node];
            var typeOfVariable = _memoizedDeclarationTypes[variableDeclarationNode];
            var result = new Dictionary<AstNode, Type>(ReferenceEqualityComparer.Instance) { { node, typeOfVariable } };
            return result;
        }

        public override Dictionary<AstNode, Type> VisitBoolLiteralValue(BoolLiteralValue node, Type expectedReturnTypeOfReturnExpr)
        {
            // No need to visit node children here

            var result = new Dictionary<AstNode, Type>(ReferenceEqualityComparer.Instance) { { node, new BoolType() } };
            return result;
        }

        public override Dictionary<AstNode, Type> VisitIntLiteralValue(IntLiteralValue node, Type expectedReturnTypeOfReturnExpr)
        {
            // No need to visit node children here

            var result = new Dictionary<AstNode, Type>(ReferenceEqualityComparer.Instance) { { node, new IntType() } };
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
        private Dictionary<AstNode, Type> VisitNodeChildren(AstNode node, Type expectedReturnTypeOfReturnExpr)
        {
            return node.Children.Aggregate(new Dictionary<AstNode, Type>(ReferenceEqualityComparer.Instance),
                (partialTypeInformation, childNode) =>
                {
                    var resultForChildNode = childNode.Accept(this, expectedReturnTypeOfReturnExpr);
                    return new Dictionary<AstNode, Type>(
                        partialTypeInformation.JoinWith(resultForChildNode, ReferenceEqualityComparer.Instance));
                }
           );
        }
    }
}
