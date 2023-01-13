namespace sernick.Ast.Analysis.TypeChecking;
using sernick.Input;

using System.Linq;
using sernick.Diagnostics;
using sernick.Ast.Analysis.NameResolution;
using sernick.Ast.Nodes;
using sernick.Utility;
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
                if (declaredType != null && !CompatibleForAssigment(declaredType, rhsType))
                {
                    _diagnostics.Report(new TypesMismatchError(declaredType, rhsType, node.InitValue.LocationRange.Start));
                }
                else if (declaredType == null && rhsType is NullPointerType)
                {
                    // When declaring variables with value 'null' user has to specify variable type
                    _diagnostics.Report(new TypeOrInitialValueShouldBePresentError(node.LocationRange.Start));
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
                if (!CompatibleForAssigment(defaultValueType, node.Type))
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
                ReferenceEquals(
                _nameResolution.CalledFunctionDeclarations[functionCallNode],
                ExternalFunctionsInfo.ExternalFunctions[2].Definition);
            if (functionCallIsANewFunctionCall)
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

            var ifStatementType = typeOfTrueBranch;
            if (node.ElseBlock != null)
            {
                var typeOfFalseBranch = childrenTypes[node.ElseBlock];

                // When typeOfFalseBranch is more specific then typeOfTrueBranch
                // use typeOfFalseBranch as a type of whole if-statement
                if (CompatibleForAssigment(typeOfFalseBranch, typeOfTrueBranch))
                {
                    ifStatementType = typeOfFalseBranch;
                }

                // Otherwise typeOfTrueBranch has to be more specific then typeOfFalseBranch.
                // If it isn't then types aren compatible
                else if (!CompatibleForAssigment(typeOfTrueBranch, typeOfFalseBranch))
                {
                    _diagnostics.Report(new UnequalBranchTypeError(typeOfTrueBranch, typeOfFalseBranch, node.LocationRange.Start));
                }
            }

            return AddTypeInformation(childrenTypes, node, ifStatementType);
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

        public override Dictionary<AstNode, Type> VisitAssignment(Assignment node, Type expectedReturnTypeOfReturnExpr)
        {
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);
            var typeOfLeftSide = childrenTypes[node.Left];

            // we may additionally improve typeOfLeftSide if we have more type info on that
            if (_nameResolution.AssignedVariableDeclarations.TryGetValue(node, out var variableDeclarationNode))
            {
                typeOfLeftSide = _memoizedDeclarationTypes[variableDeclarationNode];
            }

            var typeOfRightSide = childrenTypes[node.Right];

            if (!CompatibleForAssigment(typeOfLeftSide, typeOfRightSide))
            {
                _diagnostics.Report(new TypesMismatchError(typeOfLeftSide, typeOfRightSide, node.Right.LocationRange.Start));
            }

            // Sometimes, rhs type is more specific than lhs type, but we deliberately ignore this information in the "return"
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

        public override Dictionary<AstNode, Type> VisitNullPointerLiteralValue(NullPointerLiteralValue node, Type expectedReturnTypeOfReturnExpr) =>
            CreateTypeInformation<NullPointerType>(node);

        public override Dictionary<AstNode, Type> VisitStructFieldInitializer(StructFieldInitializer node, Type expectedReturnTypeOfReturnExpr)
        {
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);
            return AddTypeInformation(childrenTypes, node, childrenTypes[node.Value]);
        }

        public override Dictionary<AstNode, Type> VisitStructDeclaration(StructDeclaration node, Type expectedReturnTypeOfReturnExpr)
        {
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);

            var declaredFields = new Dictionary<string, FieldDeclaration>();
            foreach (var field in node.Fields)
            {
                if (field.Type is StructType fieldStruct &&
                    ReferenceEquals(_nameResolution.StructDeclarations[fieldStruct.Struct], node))
                {
                    _diagnostics.Report(new RecursiveStructDeclaration(field));
                }

                if (!declaredFields.TryAdd(field.Name.Name, field))
                {
                    var prevField = declaredFields[field.Name.Name];
                    _diagnostics.Report(new DuplicateFieldDeclaration(prevField, field));
                }
            }

            return AddTypeInformation<UnitType>(childrenTypes, node);
        }

        public override Dictionary<AstNode, Type> VisitStructValue(StructValue node, Type expectedReturnTypeOfReturnExpr)
        {
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);
            var structType = new StructType(node.StructName);

            var structDeclaration = _nameResolution.StructDeclarations[node.StructName];

            var initializedFields = new Dictionary<string, Range<ILocation>>();
            var missingFields = structDeclaration.Fields
                .Select(field => field.Name.Name)
                .ToHashSet();

            foreach (var (fieldName, valueExpression, locationRange) in node.Fields)
            {
                var fieldDeclaration = structDeclaration.Fields
                    .FirstOrDefault(declaration => declaration.Name.Name == fieldName.Name);

                if (!initializedFields.TryAdd(fieldName.Name, locationRange))
                {
                    _diagnostics.Report(new DuplicateFieldInitialization(
                        fieldName.Name,
                        First: initializedFields[fieldName.Name],
                        Second: locationRange)
                    );
                }

                missingFields.Remove(fieldName.Name);

                if (fieldDeclaration is null)
                {
                    _diagnostics.Report(new FieldNotPresentInStructError(
                        structType, fieldName, locationRange.Start)
                    );
                    continue;
                }

                var requiredType = fieldDeclaration.Type;
                var providedType = childrenTypes[valueExpression];

                if (!CompatibleForAssigment(requiredType, providedType))
                {
                    _diagnostics.Report(new TypesMismatchError(
                        requiredType,
                        providedType,
                        valueExpression.LocationRange.Start)
                    );
                }
            }

            foreach (var uninitializedField in missingFields)
            {
                _diagnostics.Report(new MissingFieldInitialization(
                    structType,
                    uninitializedField,
                    node.LocationRange.End)
                );
            }

            return AddTypeInformation(childrenTypes, node, structType);
        }

        public override Dictionary<AstNode, Type> VisitFieldDeclaration(FieldDeclaration node, Type expectedReturnTypeOfReturnExpr)
        {
            return CreateTypeInformation(node, node.Type);
        }

        public override Dictionary<AstNode, Type> VisitStructFieldAccess(StructFieldAccess node, Type expectedReturnTypeOfReturnExpr)
        {
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);
            var leftType = childrenTypes[node.Left];

            // handle auto-dereference of struct pointers e.g. structPointer.field to be equivalent to *structPointer.field
            if (leftType is PointerType pointerType)
            {
                return HandleAutomaticStructPointerDereference(node, pointerType, childrenTypes);
            }
            else if (leftType is StructType structType)
            {
                return HandleRegularStructFieldAccess(node, structType, childrenTypes);
            }
            else
            {
                _diagnostics.Report(new NotAStructTypeError(leftType, node.Left.LocationRange.Start));
                return AddTypeInformation<AnyType>(childrenTypes, node);
            }
        }

        private Dictionary<AstNode, Type> HandleAutomaticStructPointerDereference(StructFieldAccess node, PointerType nodeType, Dictionary<AstNode, Type> childrenTypes)
        {
            if (nodeType.Type is StructType structType)
            {
                return HandleRegularStructFieldAccess(node, structType, childrenTypes);
            }
            else
            {
                _diagnostics.Report(new CannotAutoDereferenceNotAStructPointer(nodeType, node.LocationRange.Start));
                return AddTypeInformation<AnyType>(childrenTypes, node);
            }
        }

        private Dictionary<AstNode, Type> HandleRegularStructFieldAccess(StructFieldAccess node, StructType structType, Dictionary<AstNode, Type> childrenTypes)
        {
            var structDeclaration = _nameResolution.StructDeclarations[structType.Struct];

            var fieldName = node.FieldName;
            var fieldDeclaration = structDeclaration.Fields.FirstOrDefault(fieldDeclaration => fieldDeclaration.Name.Name == fieldName.Name);

            if (fieldDeclaration is not null)
            {
                return CreateTypeInformation(node, fieldDeclaration.Type);
            }

            _diagnostics.Report(new FieldNotPresentInStructError(structType, node.FieldName, node.FieldName.LocationRange.Start));
            return AddTypeInformation<AnyType>(childrenTypes, node);
        }

        public override Dictionary<AstNode, Type> VisitPointerDereference(PointerDereference node, Type expectedReturnTypeOfReturnExpr)
        {
            var childrenTypes = VisitNodeChildren(node, expectedReturnTypeOfReturnExpr);
            var underlyingExpressionType = childrenTypes[node.Pointer];

            // TODO check if we have tests for that
            if (underlyingExpressionType is not PointerType pointerType)
            {
                _diagnostics.Report(new CannotDereferenceExpressionError(underlyingExpressionType, node.LocationRange.Start));
                return AddTypeInformation<AnyType>(childrenTypes, node);
            }

            return AddTypeInformation(childrenTypes, node, pointerType.Type);

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

        private bool Same(Type lhsType, Type rhsType)
        {
            if (lhsType is AnyType)
            {
                return true;
            }

            if (rhsType is AnyType)
            {
                return true;
            }

            return lhsType switch
            {
                StructType lhsStruct => rhsType is StructType rhsStruct && ReferenceEquals(
                    _nameResolution.StructDeclarations[lhsStruct.Struct],
                    _nameResolution.StructDeclarations[rhsStruct.Struct]),
                PointerType lhsPointer => rhsType is PointerType rhsPointer && Same(lhsPointer.Type, rhsPointer.Type),
                _ => lhsType == rhsType
            };
        }

        private bool CompatibleForAssigment(Type lhsType, Type rhsType)
        {
            // first, handle some special cases
            if (rhsType is NullPointerType && lhsType is PointerType)
            {
                return true;
            }

            // defer to "Same" in general case
            return Same(lhsType, rhsType);
        }
    }
}
