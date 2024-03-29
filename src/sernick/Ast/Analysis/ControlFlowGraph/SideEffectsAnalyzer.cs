namespace sernick.Ast.Analysis.ControlFlowGraph;

using CallGraph;
using Compiler.Function;
using FunctionContextMap;
using NameResolution;
using Nodes;
using sernick.ControlFlowGraph.CodeTree;
using StructProperties;
using TypeChecking;
using Utility;
using VariableAccess;
using static sernick.ControlFlowGraph.CodeTree.StructHelper;
using AstFunctionCall = Nodes.FunctionCall;
using CodeTreeFunctionCall = sernick.ControlFlowGraph.CodeTree.FunctionCall;
using SideEffectsVisitorParam = Utility.Unit;
using Variable = Nodes.VariableDeclaration;

public static class SideEffectsAnalyzer
{
    public static IReadOnlyList<SingleExitNode> PullOutSideEffects(
        AstNode root,
        NameResolutionResult nameResolution,
        IFunctionContext currentFunctionContext,
        FunctionContextMap functionContextMap,
        CallGraph callGraph,
        VariableAccessMap variableAccessMap,
        StructProperties structProperties,
        TypeCheckingResult typeCheckingResult
    )
    {
        var visitor = new SideEffectsVisitor(nameResolution, currentFunctionContext, functionContextMap, callGraph, variableAccessMap, typeCheckingResult, structProperties);
        var visitorResult = root.Accept(visitor, Unit.I);

        if (!visitorResult.Any())
        {
            return new List<SingleExitNode>();
        }

        var readVariables = new HashSet<Variable>();
        var writtenVariables = new HashSet<Variable>();
        var operations = new List<CodeTreeNode>();

        var result = new List<SingleExitNode>();

        foreach (var tree in visitorResult)
        {
            if (operations.Count != 0 && (
                    !tree.CanMerge
                    || tree.Affects(readVariables)
                    || tree.AffectedBy(writtenVariables)))
            {
                result.Add(new SingleExitNode(null, operations));
                readVariables = new HashSet<Variable>();
                writtenVariables = new HashSet<Variable>();
                operations = new List<CodeTreeNode>();
            }

            operations.Add(tree.CodeTree);
            readVariables.UnionWith(tree.ReadVariables);
            writtenVariables.UnionWith(tree.WrittenVariables);
            if (!tree.CanMerge)
            {
                result.Add(new SingleExitNode(null, operations));
                readVariables = new HashSet<Variable>();
                writtenVariables = new HashSet<Variable>();
                operations = new List<CodeTreeNode>();
            }
        }

        if (operations.Count > 0)
        {
            result.Add(new SingleExitNode(null, operations));
        }

        return result;
    }

    private record TreeWithEffects
    (
        HashSet<Variable> ReadVariables,
        HashSet<Variable> WrittenVariables,
        CodeTreeNode CodeTree,
        bool CanMerge = true
    )
    {
        public TreeWithEffects(CodeTreeNode codeTree, bool canMerge = true)
            : this(
                new HashSet<VariableDeclaration>(),
                new HashSet<VariableDeclaration>(),
                codeTree,
                canMerge)
        { }

        public bool AffectedBy(IReadOnlySet<VariableDeclaration> variableWrites) =>
            ReadVariables.Overlaps(variableWrites) || WrittenVariables.Overlaps(variableWrites);

        public bool AffectedBy(IEnumerable<TreeWithEffects> trees) =>
            trees.Any(tree => AffectedBy(tree.WrittenVariables));

        public bool Affects(IEnumerable<VariableDeclaration> variableReads) =>
            WrittenVariables.Overlaps(variableReads);

        public bool Affects(IEnumerable<TreeWithEffects> trees) =>
            trees.Any(tree => Affects(tree.ReadVariables));
    }

    private class SideEffectsVisitor : AstVisitor<List<TreeWithEffects>, SideEffectsVisitorParam>
    {
        public SideEffectsVisitor(
            NameResolutionResult nameResolution,
            IFunctionContext currentFunctionContext,
            FunctionContextMap functionContextMap,
            CallGraph callGraph,
            VariableAccessMap variableAccessMap,
            TypeCheckingResult typeChecking,
            StructProperties structProperties)
        {
            _nameResolution = nameResolution;
            _currentFunctionContext = currentFunctionContext;
            _functionContextMap = functionContextMap;
            _callGraph = callGraph;
            _variableAccessMap = variableAccessMap;
            _structProperties = structProperties;
            _typeChecking = typeChecking;

            _structHelper = new StructHelper(structProperties, nameResolution);
        }

        protected override List<TreeWithEffects> VisitAstNode(AstNode node, SideEffectsVisitorParam param)
        {
            var results = node.Children.SelectMany(child => child.Accept(this, param));

            return results.ToList();
        }

        protected override List<TreeWithEffects> VisitFlowControlStatement(FlowControlStatement node, SideEffectsVisitorParam param)
        {
            throw new NotSupportedException("Side effects analysis expects an AST with linear flow");
        }

        public override List<TreeWithEffects> VisitIdentifier(Identifier node, SideEffectsVisitorParam param)
        {
            throw new NotSupportedException("Side effects analysis shouldn't descend into Identifiers");
        }

        public override List<TreeWithEffects> VisitVariableDeclaration(VariableDeclaration node, SideEffectsVisitorParam param)
        {
            return GenerateVariableAssignmentTree(node, node.InitValue ?? new EmptyExpression(node.LocationRange), param);
        }

        public override List<TreeWithEffects> VisitFunctionParameterDeclaration(FunctionParameterDeclaration node, SideEffectsVisitorParam param)
        {
            throw new NotSupportedException("Side effects analysis shouldn't descend into function parameter declaration");
        }

        public override List<TreeWithEffects> VisitFunctionDefinition(FunctionDefinition node, SideEffectsVisitorParam param)
        {
            // we don't descend into function definitions,
            // since CFG should be calculated for each function separately
            return new List<TreeWithEffects>();
        }

        public override List<TreeWithEffects> VisitFunctionCall(AstFunctionCall node, SideEffectsVisitorParam param)
        {
            var definition = _nameResolution.CalledFunctionDeclarations[node];
            var parameterList = definition.Parameters.ToList();
            var args = node.Arguments.ToList();
            var argsEvals = args.Select(arg => arg.Accept(this, param)).ToList();
            var argsValues = argsEvals.Select(result =>
            {
                if (result.Count == 0)
                {
                    throw new NotSupportedException("Arguments must be non-empty expressions");
                }

                if (result.Last().CodeTree is not CodeTreeValueNode argValue)
                {
                    throw new NotSupportedException("Argument expressions must evaluate to values");
                }

                return argValue;
            }).ToList();

            for (var i = 0; i < argsEvals.Count; i++)
            {
                var currentArgValue = argsEvals[i].Last();

                if (parameterList[i].Type is StructType argType)
                {
                    var structSize = _structProperties.StructSizes[argType.Struct];
                    var copyLocation = GenerateStructTemporary(_nameResolution.StructDeclarations[argType.Struct], parameterList[i]);
                    var structAddress = argsValues[i];
                    var copyOps = GenerateStructCopy(copyLocation, structAddress, structSize);
                    var copyOpsWithEffects = copyOps.Select(x => new TreeWithEffects(x, false));
                    argsEvals[i].RemoveAt(argsEvals[i].Count - 1);
                    argsEvals[i] = argsEvals[i].Concat(copyOpsWithEffects).ToList();
                    argsValues[i] = copyLocation;
                    continue;
                }

                for (var j = i + 1; j < argsEvals.Count; j++)
                {
                    var followingArgEval = argsEvals[j];
                    if (!currentArgValue.AffectedBy(followingArgEval) && !currentArgValue.Affects(followingArgEval))
                    {
                        continue;
                    }

                    var (tempRead, tempWrite) = GenerateTemporary(argsValues[i], args[i]);
                    argsEvals[i][^1] = currentArgValue with { CodeTree = tempWrite };
                    argsValues[i] = tempRead;
                    break;
                }

                if (ReferenceEquals(argsEvals[i].Last(), currentArgValue))
                {
                    argsEvals[i].RemoveAt(argsEvals[i].Count - 1);
                }
            }

            CodeTreeValueNode? returnStructAddress = null;
            if (definition.ReturnType is StructType retType)
            {
                returnStructAddress = GenerateStructTemporary(_nameResolution.StructDeclarations[retType.Struct], node);
                argsValues = argsValues.Prepend(returnStructAddress).ToList();
            }

            var (functionCall, resultLocation) = _functionContextMap.Callers[node].GenerateCall(argsValues);

            if (returnStructAddress is not null)
            {
                resultLocation = returnStructAddress;
            }

            var functionCallTrees = functionCall.SelectMany(
                callTree => callTree.Operations
                    .Select((tree, index) => new TreeWithEffects(tree, index != 0)))
                    .ToList();

            functionCallTrees = functionCallTrees
                .Select(tree => AddFunctionCallSideEffects(tree, node))
                .ToList();

            // the called function has no arguments - we may merge the call
            // with some previous code tree (assuming no visible variable access)
            if (functionCallTrees[0].CodeTree is CodeTreeFunctionCall)
            {
                functionCallTrees[0] = functionCallTrees[0] with { CanMerge = true };
            }
            // the called function doesn't return a value - we may merge the call
            // with some next code tree (assuming no visible variable access)
            else if (functionCallTrees[^1].CodeTree is CodeTreeFunctionCall)
            {
                functionCallTrees[^1] = functionCallTrees[^1] with { CanMerge = true };
            }

            var resultTree = resultLocation != null ? new TreeWithEffects(resultLocation, false) : null;

            var operations = argsEvals.SelectMany(trees => trees).Concat(functionCallTrees).ToList();
            if (resultTree != null)
            {
                operations.Add(resultTree);
            }

            return operations;
        }

        public override List<TreeWithEffects> VisitInfix(Infix node, SideEffectsVisitorParam param)
        {
            var leftResult = node.Left.Accept(this, param);
            var rightResult = node.Right.Accept(this, param);

            if (leftResult.Count == 0 || rightResult.Count == 0)
            {
                throw new NotSupportedException("Left- and right-hand sides of a binary operation can't be empty");
            }

            if (leftResult[^1].CodeTree is not CodeTreeValueNode leftValue
                || rightResult[^1].CodeTree is not CodeTreeValueNode rightValue)
            {
                throw new NotSupportedException("Left- and right-hand sides of a binary operation have to be values");
            }

            // If the evaluation of `rightResult` affects `leftValue`,
            // we can't reorder `leftValue` after `rightResult`.
            // Write to a temporary to be read at the end.
            var rightReads = new HashSet<Variable>();
            var rightWrites = new HashSet<Variable>();
            foreach (var tree in rightResult.SkipLast(1))
            {
                rightReads.UnionWith(tree.ReadVariables);
                rightWrites.UnionWith(tree.WrittenVariables);
            }

            if (leftResult[^1].AffectedBy(rightWrites)
                || leftResult[^1].Affects(rightReads))
            {
                var (tempRead, tempWrite) = GenerateTemporary(leftValue, node);
                leftResult[^1] = leftResult[^1] with { CodeTree = tempWrite };
                leftResult.Add(new TreeWithEffects(tempRead));
                leftValue = tempRead;
            }

            var operationResult = node.Operator switch
            {
                Infix.Op.Plus => new BinaryOperationNode(BinaryOperation.Add, leftValue, rightValue),
                Infix.Op.Minus => new BinaryOperationNode(BinaryOperation.Sub, leftValue, rightValue),
                Infix.Op.Less => new BinaryOperationNode(BinaryOperation.LessThan, leftValue, rightValue),
                Infix.Op.Greater => new BinaryOperationNode(BinaryOperation.GreaterThan, leftValue, rightValue),
                Infix.Op.LessOrEquals => new BinaryOperationNode(BinaryOperation.LessThanEqual, leftValue, rightValue),
                Infix.Op.GreaterOrEquals => new BinaryOperationNode(BinaryOperation.GreaterThanEqual, leftValue, rightValue),
                Infix.Op.Equals => new BinaryOperationNode(BinaryOperation.Equal, leftValue, rightValue),
                Infix.Op.NotEqual => new BinaryOperationNode(BinaryOperation.NotEqual, leftValue, rightValue),
                Infix.Op.ScAnd or Infix.Op.ScOr => throw new NotSupportedException(
                    "Side effects analysis expects an AST with linear flow"),
                _ => throw new ArgumentOutOfRangeException()
            };

            var readVariables = new HashSet<VariableDeclaration>();
            readVariables.UnionWith(leftResult[^1].ReadVariables);
            readVariables.UnionWith(rightResult[^1].ReadVariables);
            var writtenVariables = new HashSet<VariableDeclaration>();

            var operationTree = new TreeWithEffects
            (
                readVariables,
                writtenVariables,
                operationResult
            );

            var result = leftResult.SkipLast(1).ToList();
            result.AddRange(rightResult.SkipLast(1));
            result.Add(operationTree);
            return result;
        }

        public override List<TreeWithEffects> VisitAssignment(Assignment node, SideEffectsVisitorParam param)
        {
            switch (node.Left)
            {
                case StructFieldAccess fieldAccess:
                    return GenerateStructFieldAssignmentTree(fieldAccess, node.Right, param);
                case VariableValue variableValue:
                    var declaration = _nameResolution.UsedVariableDeclarations[variableValue];
                    if (declaration is VariableDeclaration variableDeclaration)
                    {
                        return GenerateVariableAssignmentTree(variableDeclaration, node.Right, param);
                    }

                    break;
            }

            throw new NotSupportedException($"Invalid lvalue for assignment {node}");
        }

        public override List<TreeWithEffects> VisitVariableValue(VariableValue node, SideEffectsVisitorParam param)
        {
            var variable = _nameResolution.UsedVariableDeclarations[node];
            var readVariables = new HashSet<VariableDeclaration>();
            if (variable is VariableDeclaration declaration)
            {
                readVariables.Add(declaration);
            }

            var variableReadTree = _currentFunctionContext.GenerateVariableRead(variable);
            return new List<TreeWithEffects>
            {
                new (
                    readVariables,
                    new HashSet<VariableDeclaration>(),
                     variableReadTree
                )
            };
        }

        public override List<TreeWithEffects> VisitBoolLiteralValue(BoolLiteralValue node, SideEffectsVisitorParam param)
        {
            return new List<TreeWithEffects>
            {
                new (
                    new HashSet<VariableDeclaration>(),
                    new HashSet<VariableDeclaration>(),
                     new Constant(new RegisterValue(Convert.ToInt64(node.Value)))
                )
            };
        }

        public override List<TreeWithEffects> VisitIntLiteralValue(IntLiteralValue node, SideEffectsVisitorParam param)
        {
            return new List<TreeWithEffects>
            {
                new (
                    new HashSet<VariableDeclaration>(),
                    new HashSet<VariableDeclaration>(),
                     new Constant(new RegisterValue(node.Value))
                )
            };
        }

        public override List<TreeWithEffects> VisitEmptyExpression(EmptyExpression node, SideEffectsVisitorParam param)
        {
            return new List<TreeWithEffects>();
        }

        public override List<TreeWithEffects> VisitStructDeclaration(StructDeclaration node, Unit param)
        {
            return new List<TreeWithEffects>();
        }

        public override List<TreeWithEffects> VisitStructValue(StructValue node, Unit param)
        {
            var structType = (StructType)_typeChecking.ExpressionsTypes[node];
            var structDeclaration = _nameResolution.StructDeclarations[node.StructName];
            var temp = GenerateStructTemporary(structDeclaration, node);

            var result = new List<TreeWithEffects>();

            foreach (var (fieldName, expression, _) in node.Fields)
            {
                var field = _structHelper.GetStructFieldDeclaration(structType, fieldName);
                var fieldResult = expression.Accept(this, param);

                var last = fieldResult[^1];
                if (last.CodeTree is not CodeTreeValueNode value)
                {
                    throw new NotSupportedException("Internal error");
                }

                fieldResult.RemoveAt(fieldResult.Count - 1);
                var fieldWrite = _structHelper.GenerateStructFieldWrite(temp, value, field);
                result.AddRange(fieldResult);
                result.AddRange(fieldWrite.Select(tree => last with { CodeTree = tree }));
            }

            result.Add(new TreeWithEffects(temp));

            return result;
        }

        public override List<TreeWithEffects> VisitStructFieldAccess(StructFieldAccess node, Unit param)
        {
            var result = node.Left.Accept(this, param);
            // NOTE: for pointer types, add derefs here
            var structType = (StructType)_typeChecking.ExpressionsTypes[node.Left];
            var field = _structHelper.GetStructFieldDeclaration(structType, node.FieldName);
            if (result[^1].CodeTree is not CodeTreeValueNode value)
            {
                throw new NotSupportedException("Internal error");
            }

            result[^1] = result[^1] with { CodeTree = _structHelper.GenerateStructFieldRead(value, field) };
            return result;
        }

        private List<TreeWithEffects> GenerateVariableAssignmentTree(VariableDeclaration variable, AstNode value, SideEffectsVisitorParam param)
        {
            var result = value.Accept(this, param);
            if (result.Count == 0)
            {
                // the right side of assignment is an empty expression,
                // so this is a unit assignment.
                // There's nothing to do.
                return new List<TreeWithEffects>();
            }

            var last = result[^1];

            if (last.CodeTree is not CodeTreeValueNode assignedValue)
            {
                throw new NotSupportedException("Right-hand side of assignment should be a value");
            }

            last.WrittenVariables.Add(variable);

            if (!_currentFunctionContext.IsVariableStruct(variable))
            {
                result[^1] = last with
                {
                    CodeTree = _currentFunctionContext.GenerateVariableWrite(variable, assignedValue)
                };
                return result;
            }

            var targetStruct = _currentFunctionContext.GenerateVariableRead(variable);
            var targetSize = _currentFunctionContext.LocalVariableSize[variable];

            return result.SkipLast(1).Concat(StructHelper.GenerateStructCopy(targetStruct, assignedValue, targetSize)
                .Select(tree => last with { CodeTree = tree })).ToList();
        }

        private List<TreeWithEffects> GenerateStructFieldAssignmentTree(StructFieldAccess node, AstNode value,
            SideEffectsVisitorParam param)
        {
            var structType = (StructType)_typeChecking.ExpressionsTypes[node.Left];
            var structDeclaration = _nameResolution.StructDeclarations[structType.Struct];
            var field = _structHelper.GetStructFieldDeclaration(structType, node.FieldName);

            var result = value.Accept(this, param);
            if (result[^1].CodeTree is not CodeTreeValueNode resultValue)
            {
                throw new NotSupportedException("Internal error");
            }

            // NOTE: we assume this has no side effects.
            // Once we add pointers, we have to store either this
            // or value in a temporary, to avoid conflicts
            var structLocationResult = node.Left.Accept(this, param);

            if (structLocationResult[^1].CodeTree is not CodeTreeValueNode structLocationValue)
            {
                throw new NotSupportedException("Internal error");
            }

            var resultWrite =
                _structHelper.GenerateStructFieldWrite(structLocationValue, resultValue, field);

            return structLocationResult.SkipLast(1)
                .Concat(result.SkipLast(1))
                .Concat(resultWrite
                    .Select(tree =>
                        new TreeWithEffects(result[^1].ReadVariables, structLocationResult[^1].ReadVariables, tree)))
                .ToList();
        }

        private (CodeTreeValueNode, CodeTreeNode) GenerateTemporary(CodeTreeValueNode value, AstNode node)
        {
            var tempVariable = new VariableDeclaration(
                new Identifier("TempVar@" + node.GetHashCode(), node.LocationRange),
                null,
                null,
                false,
                node.LocationRange);
            _currentFunctionContext.AddLocal(tempVariable);
            var tempRead = _currentFunctionContext.GenerateVariableRead(tempVariable);
            var tempWrite = _currentFunctionContext.GenerateVariableWrite(tempVariable, value);
            return (tempRead, tempWrite);
        }

        private CodeTreeValueNode GenerateStructTemporary(StructDeclaration type, AstNode node)
        {
            var structSize = _structProperties.StructSizesDeclarations[type];
            var tempVariable = new VariableDeclaration(
                new Identifier($"Temp{type.Name}Var@{node.GetHashCode()}", node.LocationRange),
                new StructType(type.Name),
                null,
                false,
                node.LocationRange);
            _currentFunctionContext.AddLocal(tempVariable, size: structSize, isStruct: true);
            var tempRead = _currentFunctionContext.GenerateVariableRead(tempVariable);
            return tempRead;
        }

        private TreeWithEffects AddFunctionCallSideEffects(TreeWithEffects tree, AstFunctionCall node)
        {
            if (tree.CodeTree is not CodeTreeFunctionCall)
            {
                return tree;
            }

            var calledFunctionDefinition = _nameResolution.CalledFunctionDeclarations[node];
            var writtenVariables = new HashSet<Variable>();
            var readVariables = new HashSet<Variable>();

            var accessibleFunctions = _callGraph.Graph[calledFunctionDefinition];

            foreach (var function in accessibleFunctions)
            {
                var accessedVariables = _variableAccessMap[function];
                foreach (var (variable, accessMode) in accessedVariables)
                {
                    if (variable is not VariableDeclaration variableDeclaration)
                    {
                        continue;
                    }

                    switch (accessMode)
                    {
                        case VariableAccessMode.ReadOnly:
                            readVariables.Add(variableDeclaration);
                            break;
                        case VariableAccessMode.WriteAndRead:
                            readVariables.Add(variableDeclaration);
                            writtenVariables.Add(variableDeclaration);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            return tree with { WrittenVariables = writtenVariables, ReadVariables = readVariables };
        }

        private readonly NameResolutionResult _nameResolution;
        private readonly IFunctionContext _currentFunctionContext;
        private readonly FunctionContextMap _functionContextMap;
        private readonly CallGraph _callGraph;
        private readonly VariableAccessMap _variableAccessMap;
        private readonly StructProperties _structProperties;
        private readonly StructHelper _structHelper;
        private readonly TypeCheckingResult _typeChecking;
    }
}
