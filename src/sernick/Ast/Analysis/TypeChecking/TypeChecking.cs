namespace sernick.Ast.Analysis.TypeChecking;

using System.Linq;
using Diagnostics;
using NameResolution;
using Nodes;
using Utility;
using TypeInformation = Dictionary<Nodes.AstNode, Type>;

public sealed record TypeCheckingResult(TypeInformation ExpressionsTypes)
{
    public Type this[AstNode node] => ExpressionsTypes[node];
}

public static class TypeChecking
{
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

        protected override Dictionary<AstNode, Type> VisitAstNode(AstNode node, Type expectedReturnTypeOfReturnExpr) =>
            node.Accept(this, expectedReturnTypeOfReturnExpr);

        public override Dictionary<AstNode, Type> VisitIdentifier(Identifier node, Type _) =>
            CreateTypeInformation<UnitType>(node);

        public override Dictionary<AstNode, Type> VisitVariableDeclaration(VariableDeclaration node, Type expectedReturnTypeOfReturnExpr)
        {
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);

            var declaredType = node.Type;
            if (node.InitValue != null)
            {
                var rhsType = childrenTypes[node.InitValue];
                if (declaredType != null && !Same(declaredType, rhsType))
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
            return AddTypeInformation<UnitType>(childrenTypes, node);
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
                if (!Same(defaultValueType, node.Type))
                {
                    _diagnostics.Report(new TypesMismatchError(node.Type, defaultValueType, node.DefaultValue.LocationRange.Start));
                }
            }

            return AddTypeInformation<UnitType>(childrenTypes, node);
        }

        public override Dictionary<AstNode, Type> VisitFunctionDefinition(FunctionDefinition node, Type expectedReturnTypeOfReturnExpr)
        {
            var declaredReturnType = node.ReturnType;
            var childrenTypes = VisitNodeChildren(node, declaredReturnType);

            var bodyReturnType = childrenTypes[node.Body];

            if (!Same(declaredReturnType, bodyReturnType))
            {
                _diagnostics.Report(new InferredBadFunctionReturnType(declaredReturnType, bodyReturnType, node.Body.Inner.LocationRange.Start));
            }

            return AddTypeInformation<UnitType>(childrenTypes, node);
        }

        public override Dictionary<AstNode, Type> VisitCodeBlock(CodeBlock node, Type expectedReturnTypeOfReturnExpr)
        {
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);

            // simply return what expression inside returns
            return AddTypeInformation(childrenTypes, node, childrenTypes[node.Inner]);
        }

        public override Dictionary<AstNode, Type> VisitExpressionJoin(ExpressionJoin node, Type expectedReturnTypeOfReturnExpr)
        {
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);

            var result = new Dictionary<AstNode, Type>(childrenTypes, ReferenceEqualityComparer.Instance);

            // return/break/continue with a semicolon makes the type of Join to be irrelevant
            if (childrenTypes[node.First] is AnyType && node.Second is EmptyExpression)
            {
                result[node] = new AnyType();
            }
            else
            {
                // just return the last expressions' type
                result[node] = childrenTypes[node.Second];
            }

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
                if (!Same(actualType, expectedType))
                {
                    _diagnostics.Report(
                        new WrongFunctionArgumentError(expectedType, actualType, actualArgument.LocationRange.Start)
                    );
                }
            }

            // handle case where function is a special "new" function

            var functionCallIsANewFunctionCall =
                (_nameResolution.CalledFunctionDeclarations[functionCallNode] ==
                ExternalFunctionsInfo.ExternalFunctions[2].Definition);
            if(functionCallIsANewFunctionCall)
            {
                var inferredArgumentType = childrenTypes[actualArguments.First()];
                return AddTypeInformation(childrenTypes, functionCallNode, new PointerType(inferredArgumentType));
            }
            else
            {
                return AddTypeInformation(childrenTypes, functionCallNode, declaredReturnType);
            }
        }

        public override Dictionary<AstNode, Type> VisitContinueStatement(ContinueStatement node, Type expectedReturnTypeOfReturnExpr) =>
            CreateTypeInformation<UnitType>(node);

        public override Dictionary<AstNode, Type> VisitReturnStatement(ReturnStatement node, Type expectedReturnTypeOfReturnExpr)
        {
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);

            // Return Value is in a subtree, so its type should be already calculated by now
            var returnValueType = (node.ReturnValue != null) ? childrenTypes[node.ReturnValue] : new UnitType();
            if (!Same(returnValueType, expectedReturnTypeOfReturnExpr))
            {
                _diagnostics.Report(new ReturnTypeError(expectedReturnTypeOfReturnExpr, returnValueType, node.LocationRange.Start));
            }

            return AddTypeInformation<AnyType>(childrenTypes, node);
        }

        public override Dictionary<AstNode, Type> VisitBreakStatement(BreakStatement node, Type expectedReturnTypeOfReturnExpr) =>
            CreateTypeInformation<UnitType>(node);

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
                if (!Same(typeOfTrueBranch, typeOfFalseBranch))
                {
                    _diagnostics.Report(new UnequalBranchTypeError(typeOfTrueBranch, typeOfFalseBranch, node.LocationRange.Start));
                }
            }

            return AddTypeInformation(childrenTypes, node, typeOfTrueBranch);
        }

        /// <summary>
        /// Loop always returns a `Unit` type
        /// break/return inside the loop would exit the loop
        /// and have no effect on the loop 
        /// </summary>
        public override Dictionary<AstNode, Type> VisitLoopStatement(LoopStatement node, Type expectedReturnTypeOfReturnExpr)
        {
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);

            return AddTypeInformation<UnitType>(childrenTypes, node);
        }

        public override Dictionary<AstNode, Type> VisitEmptyExpression(EmptyExpression node, Type _) =>
            CreateTypeInformation<UnitType>(node);

        public override Dictionary<AstNode, Type> VisitInfix(Infix node, Type expectedReturnTypeOfReturnExpr)
        {
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);

            var typeOfLeftOperand = childrenTypes[node.Left];
            var typeOfRightOperand = childrenTypes[node.Right];

            if (typeOfLeftOperand is UnitType || typeOfRightOperand is UnitType)
            {
                _diagnostics.Report(new InfixOperatorTypeError(node.Operator, typeOfLeftOperand, typeOfRightOperand, node.LocationRange.Start));
                return AddTypeInformation<UnitType>(childrenTypes, node);
            }

            if (!Same(typeOfLeftOperand, typeOfRightOperand))
            {
                _diagnostics.Report(new InfixOperatorTypeError(node.Operator, typeOfLeftOperand, typeOfRightOperand, node.LocationRange.Start));

                // TODO does it make sense to return anything here? maybe a Unit type? But it could propagate the error up the tree 
                return AddTypeInformation<UnitType>(childrenTypes, node);
            }

            var commonType = typeOfLeftOperand is AnyType ? typeOfRightOperand : typeOfLeftOperand;
            var result = new Dictionary<AstNode, Type>(childrenTypes, ReferenceEqualityComparer.Instance);

            // let's cover some special cases e.g. adding two bools or shirt-circuiting two ints
            switch (node.Operator)
            {
                case Infix.Op.ScAnd:
                case Infix.Op.ScOr:
                    {
                        if (commonType is not BoolType or AnyType)
                        {
                            _diagnostics.Report(new InfixOperatorTypeError(node.Operator, typeOfLeftOperand, typeOfRightOperand, node.LocationRange.Start));
                        }

                        result.Add(node, new BoolType());
                        break;
                    }
                case Infix.Op.Plus:
                case Infix.Op.Minus:
                    {
                        if (commonType is not IntType or AnyType)
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
                        if (commonType is not IntType or AnyType)
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

        // TODO https://github.com/jwajgelt/sernick/pull/283#discussion_r1064158212
        public override Dictionary<AstNode, Type> VisitAssignment(Assignment node, Type expectedReturnTypeOfReturnExpr)
        {
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);

            var variableDeclarationNode = _nameResolution.AssignedVariableDeclarations[node];
            var typeOfLeftSide = _memoizedDeclarationTypes[variableDeclarationNode];
            var typeOfRightSide = childrenTypes[node.Right];

            if (!CompatibleForAssigment(typeOfLeftSide, typeOfRightSide))
            {
                _diagnostics.Report(new TypesMismatchError(typeOfLeftSide, typeOfRightSide, node.Right.LocationRange.Start));
            }


            // TODO if rhs type is more specific than lhs type, should we change "typeOfLeftSide" to "typeOfRightSide"
            // below, effectively being more efficient in inferring types?
            // Example: assigning a null pointer to a variable of type Int*, currently we only store that the result
            // is of type Int*, but we could be storing that the result is of type NULL_PTR, maybe preventing some bugs?
            return AddTypeInformation(childrenTypes, node, typeOfLeftSide);
        }

        public override Dictionary<AstNode, Type> VisitVariableValue(VariableValue node, Type expectedReturnTypeOfReturnExpr)
        {
            var variableDeclarationNode = _nameResolution.UsedVariableDeclarations[node];
            var typeOfVariable = _memoizedDeclarationTypes[variableDeclarationNode];
            return CreateTypeInformation(node, typeOfVariable);
        }

        public override Dictionary<AstNode, Type> VisitBoolLiteralValue(BoolLiteralValue node, Type expectedReturnTypeOfReturnExpr) =>
            CreateTypeInformation<BoolType>(node);

        public override Dictionary<AstNode, Type> VisitIntLiteralValue(IntLiteralValue node, Type expectedReturnTypeOfReturnExpr) =>
            CreateTypeInformation<IntType>(node);

        public override Dictionary<AstNode, Type> VisitStructDeclaration(StructDeclaration node, Type expectedReturnTypeOfReturnExpr)
        {
            var childrenTypes = VisitNodeChildren(node, new AnyType());
            var structTypeWithFieldTypes = new StructType(node.Name);

            structTypeWithFieldTypes.fieldTypes = node.Fields.ToDictionary(
                keySelector: fieldDeclaration => fieldDeclaration.Name,
                elementSelector: fieldDeclaration => fieldDeclaration.Type);
            
            return AddTypeInformation(childrenTypes, node, structTypeWithFieldTypes);
        }

        public override Dictionary<AstNode, Type> VisitFieldDeclaration(FieldDeclaration node, Type expectedReturnTypeOfReturnExpr)
        {
            return CreateTypeInformation(node, node.Type);
        }

        // TODO can we assume that when VisitStructFieldAccess is called, the corresponding struct declaration
        // has already been visited by VisitStructDeclaration???
        // For now, let's assume that is has.
        public override Dictionary<AstNode, Type> VisitStructFieldAccess(StructFieldAccess node, Type expectedReturnTypeOfReturnExpr)
        {
            var childrenTypes = VisitNodeChildren(node, new AnyType());

            var structType = childrenTypes[node.Left];
            if(structType is StructType)
            {
                var fieldName = node.FieldName;
                if(((StructType)structType).fieldTypes == null)
                {
                    _diagnostics.Report(new NoTypeInformationAboutStructError(node.LocationRange.Start));
                    return CreateTypeInformation<AnyType>(node);
                }
                else
                {
                    // TODO figure out c# comment about "fieldTypes" possibly being null, this should not happen :|
                    var fieldNameDeclaredInStruct = ((StructType)structType).fieldTypes.ContainsKey(fieldName);

                    if (!fieldNameDeclaredInStruct)
                    {
                        _diagnostics.Report(new FieldNotPresentInStructError(structType, node.FieldName, node.FieldName.LocationRange.Start));
                        return CreateTypeInformation<AnyType>(node);
                    }

                    return CreateTypeInformation(node, ((StructType)structType).fieldTypes[fieldName]);
                }
            }
            else
            {
                _diagnostics.Report(new NotAStructTypeError(structType, node.Left.LocationRange.Start));
                return CreateTypeInformation<AnyType>(node);
            }
        }

        public override Dictionary<AstNode, Type> VisitPointerDereference(PointerDereference node, Type _)
        {
            var childrenTypes = VisitNodeChildren(node, new AnyType());
            var underlyingExpressionType = childrenTypes[node.Pointer];

            // TODO check if we have tests for that
            if (!isPointerType(underlyingExpressionType))
            {
                _diagnostics.Report(new CannotDereferenceExpressionError(underlyingExpressionType, node.LocationRange.Start));
            }

            return AddTypeInformation(childrenTypes, node, underlyingExpressionType);

        }

        /// <summary>
        /// Since we want to do a bottom-up recursion, but we're calling our node.Accept functions
        /// in a top-down order, before actually processing a node we have to make sure we've visited all of its
        /// children. This helper method should be thus called at the beginning of almost each "Visit" function,
        /// except simple AST nodes (e.g. int literal), where we know there would be no recursion
        /// </summary>
        private Dictionary<AstNode, Type> VisitNodeChildren(AstNode node, Type expectedReturnTypeOfReturnExpr)
        {
            return node.Children.Aggregate(new Dictionary<AstNode, Type>(ReferenceEqualityComparer.Instance),
                (partialTypeInformation, childNode) =>
                {
                    var resultForChildNode = childNode.Accept(this, expectedReturnTypeOfReturnExpr);
                    return new Dictionary<AstNode, Type>(
                        partialTypeInformation.JoinWith(resultForChildNode, ReferenceEqualityComparer.Instance),
                        ReferenceEqualityComparer.Instance);
                }
           );
        }

        private static TypeInformation CreateTypeInformation<TType>(AstNode node) where TType : Type, new() =>
            CreateTypeInformation(node, new TType());

        private static TypeInformation CreateTypeInformation(AstNode node, Type type) =>
            new(ReferenceEqualityComparer.Instance) { { node, type } };

        private static TypeInformation AddTypeInformation<TType>(TypeInformation types, AstNode node)
            where TType : Type, new() =>
            AddTypeInformation(types, node, new TType());

        private static TypeInformation AddTypeInformation(TypeInformation types, AstNode node, Type type) =>
            new(types, ReferenceEqualityComparer.Instance) { { node, type } };

        private static bool Same(Type lhsType, Type rhsType)
        {
            if (lhsType is AnyType)
            {
                return true;
            }

            if (rhsType is AnyType)
            {
                return true;
            }

            return lhsType == rhsType;
        }

        private static bool CompatibleForAssigment(Type lhsType, Type rhsType)
        {
            // first, handle some special cases
            if (rhsType is NullPointerType && lhsType is PointerType)
            {
                return true;
            }

            // defer to "Same" in general case
            return Same(lhsType, rhsType);
        }

        private static bool isPointerType(Type type)
        {
            if (
                type is PointerType(IntType) ||
                type is PointerType(BoolType) ||
                type is PointerType(PointerType(IntType))
                // TODO figure out how to check it properly, since it could be e.g. ****Int
            )
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
