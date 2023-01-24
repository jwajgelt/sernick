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
using static ControlFlowAnalysisHelpers;
using FunctionCall = Nodes.FunctionCall;

public static class ControlFlowAnalyzer
{
    /// <summary>
    /// This method returns a control flow graph for a given function represented by an AST
    /// </summary>
    /// <param name="pullOutSideEffects">function that, given an AST without any control flow expressions, returns a linear list of CFG nodes</param>
    public static CodeTreeRoot UnravelControlFlow(
        FunctionDefinition functionDefinition,
        NameResolutionResult nameResolution,
        FunctionContextMap contextMap,
        CallGraph callGraph,
        VariableAccessMap variableAccessMap,
        TypeCheckingResult typeCheckingResult,
        StructProperties structProperties,
        Func<AstNode, NameResolutionResult, IFunctionContext, FunctionContextMap, CallGraph, VariableAccessMap, StructProperties, TypeCheckingResult, IReadOnlyList<SingleExitNode>>
            pullOutSideEffects
    )
    {
        var nodesWithControlFlow = new HashSet<AstNode>();
        functionDefinition.Body.Accept(new ContainsControlFlowVisitor(), nodesWithControlFlow);

        var currentFunctionContext = contextMap[functionDefinition];
        var variableFactory = new TemporaryLocalVariableFactory(currentFunctionContext);

        var visitor =
            new ControlFlowVisitor(
                currentFunctionContext,
                nodesWithControlFlow,
                (root, next, resultVariable, nameResolutionResult) =>
                {
                    var nodes = pullOutSideEffects(root, nameResolutionResult, currentFunctionContext, contextMap, callGraph, variableAccessMap, structProperties, typeCheckingResult).ToList();
                    if (nodes.Count == 0)
                    {
                        return next;
                    }

                    // if there is a returned value
                    if (nodes[^1].Operations[^1] is CodeTreeValueNode valueNode)
                    {
                        // store the value in the result variable (if there is one)
                        if (resultVariable is not null)
                        {
                            nodes[^1] = new SingleExitNode(next,
                                nodes[^1].Operations.SkipLast(1)
                                    .Concat(resultVariable.GenerateValueWrite(valueNode))
                                    .ToList());
                        }
                        // otherwise skip the value completely
                        else if (nodes[^1].Operations.Count == 1)
                        {
                            nodes = nodes.SkipLast(1).ToList();
                        }
                        else
                        {
                            nodes[^1] = new SingleExitNode(next, nodes[^1].Operations.SkipLast(1).ToList());
                        }
                    }

                    foreach (var (node, nextNode) in nodes.Zip(nodes.Skip(1)))
                    {
                        node.NextTree = nextNode;
                    }

                    if (nodes.Count == 0)
                    {
                        return next;
                    }

                    nodes[^1].NextTree = next;
                    return nodes[0];
                },
                variableFactory,
                typeCheckingResult,
                nameResolution,
                contextMap,
                structProperties
            );

        IValueLocation? resultVariable = null;
        CodeTreeValueNode? valToReturn = null;
        if (currentFunctionContext.ValueIsReturned)
        {
            resultVariable = variableFactory.NewPrimitiveVariable();
            valToReturn = resultVariable.GenerateValueRead();
        }

        var prologue = currentFunctionContext.GeneratePrologue();
        var epilogue = currentFunctionContext.GenerateEpilogue(valToReturn);

        prologue[^1].NextTree = functionDefinition.Body.Accept(visitor,
            new ControlFlowVisitorParam(
                epilogue[0],
                null,
                null,
                epilogue[0],
                resultVariable,
                resultVariable,
                false
            ));

        return prologue[0];
    }

    private sealed record ControlFlowVisitorParam
    (
        CodeTreeRoot Next, // CFG node that will be visited after the CFG for the currently processed AST 
        CodeTreeRoot? Break, // CFG node that will be visited after a break statement
        CodeTreeRoot? Continue, // CFG node that will be visited after a continue statement
        CodeTreeRoot Return, // CFG node that will be visited after a return statement
        IValueLocation? ResultVariable, // variable in which the result of the given tree should be stored
        IValueLocation? ReturnResultVariable, // variable in which the return result should be stored
        bool IsCondition
    );

    private class TemporaryLocalVariableFactory
    {
        private class TemporaryVariable : IFunctionVariable { }

        public class RegisterValueLocation : IValueLocation
        {
            private readonly IFunctionContext _functionContext;
            private readonly TemporaryVariable _temp;

            public RegisterValueLocation(IFunctionContext functionContext)
            {
                _functionContext = functionContext;
                _temp = new TemporaryVariable();
                _functionContext.AddLocal(_temp);
            }

            public IEnumerable<CodeTreeNode> GenerateValueWrite(CodeTreeValueNode value)
            {
                return _functionContext.GenerateVariableWrite(_temp, value).Enumerate();
            }

            public CodeTreeValueNode GenerateValueRead() => _functionContext.GenerateVariableRead(_temp);
        }
        private readonly IFunctionContext _functionContext;
        public TemporaryLocalVariableFactory(IFunctionContext functionContext)
        {
            _functionContext = functionContext;
        }

        public RegisterValueLocation NewPrimitiveVariable()
        {
            return new RegisterValueLocation(_functionContext);
        }

        public StructValueLocation NewStructVariable(int size)
        {
            var temp = new TemporaryVariable();
            _functionContext.AddLocal(temp, isStruct: true, size: size);
            return new StructValueLocation(new VariableValueLocation(_functionContext, temp), size);
        }
    }

    private sealed class ControlFlowVisitor : AstVisitor<CodeTreeRoot, ControlFlowVisitorParam>
    {
        private readonly Func<AstNode, CodeTreeRoot, IValueLocation?, CodeTreeRoot> _pullOutSideEffects;
        private readonly IFunctionContext _currentFunctionContext;
        private readonly IReadOnlySet<AstNode> _nodesWithControlFlow;
        private readonly TemporaryLocalVariableFactory _variableFactory;
        private readonly TypeCheckingResult _typeChecking;
        private readonly FunctionContextMap _functionContextMap;
        private readonly StructProperties _structProperties;
        private NameResolutionResult _nameResolution;
        private readonly StructHelper _structHelper;

        public ControlFlowVisitor
        (
            IFunctionContext currentFunctionContext,
            IReadOnlySet<AstNode> nodesWithControlFlow,
            Func<AstNode, CodeTreeRoot, IValueLocation?, NameResolutionResult, CodeTreeRoot> pullOutSideEffects,
            TemporaryLocalVariableFactory variableFactory,
            TypeCheckingResult typeChecking,
            NameResolutionResult nameResolution,
            FunctionContextMap functionContextMap,
            StructProperties structProperties
        )
        {
            _nameResolution = nameResolution;
            _pullOutSideEffects = (root, next, resultVariable) => pullOutSideEffects(root, next, resultVariable, _nameResolution);
            _currentFunctionContext = currentFunctionContext;
            _nodesWithControlFlow = nodesWithControlFlow;
            _variableFactory = variableFactory;
            _typeChecking = typeChecking;
            _functionContextMap = functionContextMap;
            _structProperties = structProperties;
            _structHelper = new StructHelper(structProperties, nameResolution);
        }

        protected override CodeTreeRoot VisitAstNode(AstNode node, ControlFlowVisitorParam param)
        {
            if (!_nodesWithControlFlow.Contains(node))
            {
                return _pullOutSideEffects(node, param.Next, param.ResultVariable);
            }

            var result = param.Next;
            var nextParam = param with { IsCondition = false };
            foreach (var currentNode in node.Children.Reverse())
            {
                result = currentNode.Accept(this, nextParam);
                nextParam = nextParam with { Next = result, ResultVariable = null };
            }

            return result;
        }

        public override CodeTreeRoot VisitLoopStatement(LoopStatement node, ControlFlowVisitorParam param)
        {
            var result = new SingleExitNode(null, Array.Empty<CodeTreeNode>());
            result.NextTree = node.Inner.Accept(this, param with { Next = result, Break = param.Next, Continue = result, IsCondition = false });
            return result;
        }

        public override CodeTreeRoot VisitIfStatement(IfStatement node, ControlFlowVisitorParam param)
        {
            var tempVariable = _variableFactory.NewPrimitiveVariable();

            return node.Condition.Accept(this, param with
            {
                Next = new ConditionalJumpNode(
                    node.IfBlock.Accept(this, param),
                    node.ElseBlock?.Accept(this, param) ?? param.Next,
                    tempVariable.GenerateValueRead()
                ),
                ResultVariable = tempVariable,
                IsCondition = true
            });
        }

        public override CodeTreeRoot VisitBreakStatement(BreakStatement node, ControlFlowVisitorParam param)
        {
            if (param.Break is null)
            {
                throw new ArgumentException("A break statement was encountered, but param.Break is null");
            }

            return param.Break;
        }

        public override CodeTreeRoot VisitContinueStatement(ContinueStatement node, ControlFlowVisitorParam param)
        {
            if (param.Continue is null)
            {
                throw new ArgumentException("A continue statement was encountered, but param.Continue is null");
            }

            return param.Continue;
        }

        public override CodeTreeRoot VisitReturnStatement(ReturnStatement node, ControlFlowVisitorParam param)
        {
            return node.ReturnValue?.Accept(this, param with { Next = param.Return, ResultVariable = param.ReturnResultVariable }) ?? param.Return;
        }

        public override CodeTreeRoot VisitInfix(Infix node, ControlFlowVisitorParam param)
        {
            if (!_nodesWithControlFlow.Contains(node))
            {
                return _pullOutSideEffects(node, param.Next, param.ResultVariable);
            }

            if (node.Operator is not (Infix.Op.ScAnd or Infix.Op.ScOr))
            {
                var (leftVariable, leftVariableValueNode) = GenerateTemporaryAst(node.Left);
                var (rightVariable, rightVariableValueNode) = GenerateTemporaryAst(node.Right);
                var infix = node with { Left = leftVariableValueNode, Right = rightVariableValueNode };
                return node.Left.Accept(this,
                    param with
                    {
                        Next = node.Right.Accept(this,
                            param with
                            {
                                Next = _pullOutSideEffects(infix, param.Next, param.ResultVariable),
                                ResultVariable = new VariableValueLocation(_currentFunctionContext, rightVariable)
                            }),
                        ResultVariable = new VariableValueLocation(_currentFunctionContext, leftVariable)
                    }
                );
            }

            // if this expression is a part of a if statement condition, then skip checking the condition for a second time
            CodeTreeRoot next;
            if (param.IsCondition && param.Next is ConditionalJumpNode conditionalJumpNode)
            {
                next = node.Operator is Infix.Op.ScAnd ? conditionalJumpNode.FalseCase : conditionalJumpNode.TrueCase;
            }
            else
            {
                next = param.Next;
            }

            var variable = param.ResultVariable ?? _variableFactory.NewPrimitiveVariable();
            var evaluateRight = node.Right.Accept(this, param with { ResultVariable = variable });
            var returnLeft = next;

            return node.Left.Accept(this, param with
            {
                Next = new ConditionalJumpNode(
                    node.Operator is Infix.Op.ScAnd ? evaluateRight : returnLeft,
                    node.Operator is Infix.Op.ScAnd ? returnLeft : evaluateRight,
                    variable.GenerateValueRead()
                ),
                ResultVariable = variable
            });

        }

        public override CodeTreeRoot VisitFunctionCall(FunctionCall node, ControlFlowVisitorParam param)
        {
            if (!_nodesWithControlFlow.Contains(node))
            {
                return _pullOutSideEffects(node, param.Next, param.ResultVariable);
            }

            var last = new SingleExitNode(null, Array.Empty<CodeTreeNode>());
            CodeTreeRoot result = last;
            var arguments = node.Arguments.Reverse().Select(argumentNode =>
            {
                var (tempVariable, variableValueNode) = GenerateTemporaryAst(argumentNode);
                result = argumentNode.Accept(this, param with { Next = result, ResultVariable = new VariableValueLocation(_currentFunctionContext, tempVariable), IsCondition = false });
                return variableValueNode;
            }).Reverse();

            var functionCall = node with { Arguments = arguments.ToList() };
            _nameResolution = _nameResolution.JoinWith(NameResolutionResult.OfFunctionCall(functionCall, _nameResolution.CalledFunctionDeclarations[node]));
            _functionContextMap[functionCall] = _functionContextMap[node];

            last.NextTree = _pullOutSideEffects(functionCall, param.Next, param.ResultVariable);
            return result;
        }

        public override CodeTreeRoot VisitFunctionDefinition(FunctionDefinition node, ControlFlowVisitorParam param)
        {
            // skip any function definition, because the CFG is calculated separately for every function
            return param.Next;
        }

        public override CodeTreeRoot VisitVariableDeclaration(VariableDeclaration node, ControlFlowVisitorParam param)
        {
            if (!_nodesWithControlFlow.Contains(node) || node.InitValue is null)
            {
                return _pullOutSideEffects(node, param.Next, param.ResultVariable);
            }

            if (node.Type is not StructType structType)
            {
                return node.InitValue.Accept(this,
                    param with { ResultVariable = new VariableValueLocation(_currentFunctionContext, node) });
            }

            return node.InitValue.Accept(this,
                param with { ResultVariable = new StructValueLocation(new VariableValueLocation(_currentFunctionContext, node), _structHelper.GetStructTypeSize(structType)) });
        }

        public override CodeTreeRoot VisitAssignment(Assignment node, ControlFlowVisitorParam param)
        {
            if (!_nodesWithControlFlow.Contains(node))
            {
                return _pullOutSideEffects(node, param.Next, param.ResultVariable);
            }

            // there are tree cases of lvalues:
            switch (node.Left)
            {
                case VariableValue variableAccess:
                    {
                        // assignment to variable
                        var variable = _nameResolution.UsedVariableDeclarations[variableAccess];
                        return node.Right.Accept(this,
                            param with { ResultVariable = new VariableValueLocation(_currentFunctionContext, variable) });
                    }
                case PointerDereference dereference:
                    {
                        // assigment to a de-referenced location
                        var pointerLocation = _variableFactory.NewPrimitiveVariable();
                        var dereferencedLocation = new DereferencedLocation(pointerLocation);
                        var assigment = node.Right.Accept(this, param with { ResultVariable = dereferencedLocation });
                        return dereference.Accept(this, param with { Next = assigment, ResultVariable = pointerLocation });
                    }
                case StructFieldAccess fieldAccess:
                    {
                        // field access from struct variable
                        var targetStructType = _typeChecking.ExpressionsTypes[fieldAccess.Left];
                        if (targetStructType is not StructType structType)
                        {
                            break;
                        }

                        var fieldPath = new List<FieldDeclaration>();
                        Expression structFieldAccessNode = fieldAccess;

                        // adds the rhs of field access to `fieldPath`,
                        // and continues inspecting the lhs.
                        while (structFieldAccessNode is StructFieldAccess access)
                        {
                            var lhsType = _typeChecking[access.Left] switch
                            {
                                StructType lhsStruct => lhsStruct,
                                PointerType { Type: StructType lhsStruct } => lhsStruct, // we support automatic dereference
                                _ => throw new NotSupportedException($"Invalid lvalue in assignment {node}")
                            };
                            fieldPath.Add(_structHelper.GetStructFieldDeclaration(lhsType, access.FieldName));
                            structFieldAccessNode = access.Left;
                        }

                        fieldPath.Reverse();

                        IValueLocation structLocation = structFieldAccessNode switch
                        {
                            PointerDereference => _variableFactory.NewPrimitiveVariable(),
                            VariableValue variableValue => new VariableValueLocation(_currentFunctionContext,
                                _nameResolution.UsedVariableDeclarations[variableValue]),
                            _ => throw new NotSupportedException()
                        };
                        
                        IStructValueLocation variableLocation =
                            new StructValueLocation(structLocation, _structProperties.StructSizes[structType.Struct]);

                        var isLhsPrimitiveType = _typeChecking[node.Left] is not StructType;
                        IValueLocation resultLocation;
                        if (isLhsPrimitiveType)
                        {
                            resultLocation = fieldPath
                                .SkipLast(1)
                                .Aggregate(variableLocation,
                                    (location, field) => location.GetField(_structProperties.FieldOffsets[field], _structProperties.FieldSizes[field]))

                                .GetPrimitiveField(_structProperties.FieldOffsets[fieldPath.Last()]);
                        }
                        else
                        {
                            resultLocation = fieldPath
                                .SkipLast(1)
                                .Aggregate(variableLocation,
                                    (location, field) => location.GetField(_structProperties.FieldOffsets[field], _structProperties.FieldSizes[field]
                                        ))
                                .GetField(_structProperties.FieldOffsets[fieldPath.Last()], _structProperties.FieldSizes[fieldPath.Last()]
                                    );
                        }

                        var assigment = node.Right.Accept(this, param with { ResultVariable = resultLocation });
                        if (structFieldAccessNode is PointerDereference pointerDereference)
                        {
                            // If structFieldAccessNode is a pointer dereference then we first need to calculate the struct location.
                            return pointerDereference.Accept(this,
                                param with { ResultVariable = structLocation, Next = assigment });
                        }
                        return assigment;
                    }
            }

            throw new NotSupportedException($"Invalid lvalue in assignment {node}");
        }

        public override CodeTreeRoot VisitStructValue(StructValue node, ControlFlowVisitorParam param)
        {
            if (!_nodesWithControlFlow.Contains(node))
            {
                return _pullOutSideEffects(node, param.Next, param.ResultVariable);
            }

            var structType = (StructType)_typeChecking.ExpressionsTypes[node];
            var structSize = _structHelper.GetStructTypeSize(structType);
            var tempStruct = _variableFactory.NewStructVariable(structSize);

            var copyOperations = param.ResultVariable?.GenerateValueWrite(tempStruct.GenerateValueRead()).ToList();

            var next = copyOperations == null ? param.Next : new SingleExitNode(param.Next, copyOperations);

            foreach (var (fieldName, expression, _) in node.Fields.Reverse())
            {
                var field = _structHelper.GetStructFieldDeclaration(structType, fieldName);
                var fieldOffset = _structProperties.FieldOffsets[field];

                if (field.Type is StructType)
                {
                    var fieldSize = _structHelper.GetStructFieldSize(structType, fieldName);
                    next = expression.Accept(this,
                        param with { Next = next, ResultVariable = tempStruct.GetField(fieldOffset, fieldSize) });
                }
                else
                {
                    next = expression.Accept(this,
                        param with { Next = next, ResultVariable = tempStruct.GetPrimitiveField(fieldOffset) });
                }
            }

            return next;
        }

        public override CodeTreeRoot VisitStructFieldAccess(StructFieldAccess node, ControlFlowVisitorParam param)
        {
            if (!_nodesWithControlFlow.Contains(node))
            {
                return _pullOutSideEffects(node, param.Next, param.ResultVariable);
            }

            var (leftVariable, leftVariableValueNode) = GenerateTemporaryAst(node.Left);
            var fieldAccess = node with { Left = leftVariableValueNode };

            // Not sure if this is correct but for sure this doesn't generate an efficient code.
            IValueLocation leftVariableLocation = _typeChecking[node.Left] switch
            {
                PointerType => new VariableValueLocation(_currentFunctionContext, leftVariable),
                StructType structType => new StructValueLocation(
                    new VariableValueLocation(_currentFunctionContext, leftVariable),
                    _structHelper.GetStructTypeSize(structType)),
                _ => throw new NotSupportedException()
            };

            return node.Left.Accept(this,
                param with
                {
                    Next = _pullOutSideEffects(fieldAccess, param.Next, param.ResultVariable),
                    ResultVariable = leftVariableLocation,
                }
            );
        }

        public override CodeTreeRoot VisitPointerDereference(PointerDereference node, ControlFlowVisitorParam param)
        {
            if (!_nodesWithControlFlow.Contains(node))
            {
                return _pullOutSideEffects(node, param.Next, param.ResultVariable);
            }

            var (pointerVariable, pointerNode) = GenerateTemporaryAst(node.Pointer);
            var dereference = node with { Pointer = pointerNode };
            return node.Pointer.Accept(this,
                param with
                {
                    Next = _pullOutSideEffects(dereference, param.Next, param.ResultVariable),
                    ResultVariable = new VariableValueLocation(_currentFunctionContext, pointerVariable)
                });
        }

        public override CodeTreeRoot VisitIdentifier(Identifier node, ControlFlowVisitorParam param)
        {
            throw new NotSupportedException("Control flow analysis shouldn't descend into Identifiers");
        }

        public override CodeTreeRoot VisitFunctionParameterDeclaration(FunctionParameterDeclaration node, ControlFlowVisitorParam param)
        {
            throw new NotSupportedException("Control flow analysis shouldn't descend into function parameter declaration");
        }

        private (IFunctionVariable, VariableValue) GenerateTemporaryAst(Expression node)
        {
            var variableType =
                _typeChecking.ExpressionsTypes[node];
            var identifier = new Identifier($"Temp{variableType.ToString()}@{node.GetHashCode()}", node.LocationRange);
            var tempVariable = new VariableDeclaration(
                identifier,
                variableType,
                null,
                false,
                node.LocationRange);

            if (variableType is StructType structType)
            {
                _currentFunctionContext.AddLocal(tempVariable, false, true, _structHelper.GetStructTypeSize(structType));
            }
            else
            {
                _currentFunctionContext.AddLocal(tempVariable);
            }

            var variableValue = new VariableValue(identifier, node.LocationRange);
            _nameResolution = _nameResolution.JoinWith(NameResolutionResult.OfVariableUse(variableValue, tempVariable));
            return (tempVariable, variableValue);
        }
    }

    private sealed class ContainsControlFlowVisitor : AstVisitor<bool, ISet<AstNode>>
    {
        protected override bool VisitAstNode(AstNode node, ISet<AstNode> set)
        {
            var children = node.Children.Select(childNode => childNode.Accept(this, set)).ToList();

            if (children.All(value => !value))
            {
                return false;
            }

            set.Add(node);
            return true;

        }

        public override bool VisitFunctionDefinition(FunctionDefinition definition, ISet<AstNode> set) => false;

        public override bool VisitInfix(Infix infix, ISet<AstNode> set)
        {
            var childrenResult = VisitAstNode(infix, set);
            var result = infix.Operator is Infix.Op.ScAnd or Infix.Op.ScOr || childrenResult;
            if (result)
            {
                set.Add(infix);
            }

            return result;
        }

        protected override bool VisitFlowControlStatement(FlowControlStatement node, ISet<AstNode> set)
        {
            VisitAstNode(node, set);
            set.Add(node);
            return true;
        }
    }
}
