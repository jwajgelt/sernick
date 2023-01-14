namespace sernick.Ast.Analysis.ControlFlowGraph;

using CallGraph;
using Compiler.Function;
using FunctionContextMap;
using NameResolution;
using Nodes;
using sernick.Ast.Analysis.TypeChecking;
using sernick.Ast.Analysis.VariableAccess;
using sernick.ControlFlowGraph.CodeTree;
using FunctionCall = Nodes.FunctionCall;
using static Compiler.PlatformConstants;

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
        Func<AstNode, NameResolutionResult, IFunctionContext, FunctionContextMap, CallGraph, VariableAccessMap, IReadOnlyList<SingleExitNode>>
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
                    var nodes = pullOutSideEffects(root, nameResolutionResult, currentFunctionContext, contextMap, callGraph, variableAccessMap).ToList();
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
                                    .Append(currentFunctionContext.GenerateVariableWrite(resultVariable, valueNode))
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
                contextMap
            );

        IFunctionVariable? resultVariable = null;
        CodeTreeValueNode? valToReturn = null;
        if (currentFunctionContext.ValueIsReturned)
        {
            resultVariable = variableFactory.NewVariable();
            valToReturn = currentFunctionContext.GenerateVariableRead(resultVariable);
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
        IFunctionVariable? ResultVariable, // variable in which the result of the given tree should be stored
        IFunctionVariable? ReturnResultVariable, // variable in which the return result should be stored
        bool IsCondition
    );

    private class TemporaryLocalVariable : IFunctionVariable { }

    private class TemporaryLocalVariableFactory
    {
        private readonly IFunctionContext _functionContext;
        public TemporaryLocalVariableFactory(IFunctionContext functionContext)
        {
            _functionContext = functionContext;
        }
        public IFunctionVariable NewVariable()
        {
            var temp = new TemporaryLocalVariable();
            _functionContext.AddLocal(temp, POINTER_SIZE, false);
            return temp;
        }
    }

    private sealed class ControlFlowVisitor : AstVisitor<CodeTreeRoot, ControlFlowVisitorParam>
    {
        private readonly Func<AstNode, CodeTreeRoot, IFunctionVariable?, CodeTreeRoot> _pullOutSideEffects;
        private readonly IFunctionContext _currentFunctionContext;
        private readonly IReadOnlySet<AstNode> _nodesWithControlFlow;
        private readonly TemporaryLocalVariableFactory _variableFactory;
        private readonly TypeCheckingResult _typeChecking;
        private readonly FunctionContextMap _functionContextMap;
        private NameResolutionResult _nameResolution;

        public ControlFlowVisitor
        (
            IFunctionContext currentFunctionContext,
            IReadOnlySet<AstNode> nodesWithControlFlow,
            Func<AstNode, CodeTreeRoot, IFunctionVariable?, NameResolutionResult, CodeTreeRoot> pullOutSideEffects,
            TemporaryLocalVariableFactory variableFactory,
            TypeCheckingResult typeChecking,
            NameResolutionResult nameResolution,
            FunctionContextMap functionContextMap
        )
        {
            _nameResolution = nameResolution;
            _pullOutSideEffects = (root, next, resultVariable) => pullOutSideEffects(root, next, resultVariable, _nameResolution);
            _currentFunctionContext = currentFunctionContext;
            _nodesWithControlFlow = nodesWithControlFlow;
            _variableFactory = variableFactory;
            _typeChecking = typeChecking;
            _functionContextMap = functionContextMap;
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
            var tempVariable = _variableFactory.NewVariable();

            return node.Condition.Accept(this, param with
            {
                Next = new ConditionalJumpNode(
                    node.IfBlock.Accept(this, param),
                    node.ElseBlock?.Accept(this, param) ?? param.Next,
                    _currentFunctionContext.GenerateVariableRead(tempVariable)
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
                                ResultVariable = rightVariable
                            }),
                        ResultVariable = leftVariable
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

            var variable = param.ResultVariable ?? _variableFactory.NewVariable();
            var evaluateRight = node.Right.Accept(this, param with { ResultVariable = variable });
            var returnLeft = next;

            return node.Left.Accept(this, param with
            {
                Next = new ConditionalJumpNode(
                    node.Operator is Infix.Op.ScAnd ? evaluateRight : returnLeft,
                    node.Operator is Infix.Op.ScAnd ? returnLeft : evaluateRight,
                    _currentFunctionContext.GenerateVariableRead(variable)
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
                result = argumentNode.Accept(this, param with { Next = result, ResultVariable = tempVariable, IsCondition = false });
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

            return node.InitValue.Accept(this, param with { ResultVariable = node });
        }

        public override CodeTreeRoot VisitAssignment(Assignment node, ControlFlowVisitorParam param)
        {
            if (!_nodesWithControlFlow.Contains(node))
            {
                return _pullOutSideEffects(node, param.Next, param.ResultVariable);
            }

            return node.Right.Accept(this, param with { ResultVariable = _nameResolution.AssignedVariableDeclarations[node] });
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
            var identifier = new Identifier($"TempVar@{node.GetHashCode()}", node.LocationRange);
            var tempVariable = new VariableDeclaration(
                identifier,
                _typeChecking.ExpressionsTypes[node],
                null,
                false,
                node.LocationRange);
            _currentFunctionContext.AddLocal(tempVariable, POINTER_SIZE, false);
            var variableValue = new VariableValue(identifier, node.LocationRange);
            _nameResolution = _nameResolution.JoinWith(NameResolutionResult.OfVariableUse(variableValue, tempVariable));
            return (tempVariable, variableValue);
        }
    }

    private sealed class ContainsControlFlowVisitor : AstVisitor<bool, ISet<AstNode>>
    {
        protected override bool VisitAstNode(AstNode node, ISet<AstNode> set)
        {
            if (node is FunctionDefinition)
            {
                return false;
            }

            var children = node.Children.Select(childNode => childNode.Accept(this, set)).ToList();

            if (node is not (FlowControlStatement or Infix { Operator: Infix.Op.ScAnd or Infix.Op.ScOr }) &&
                children.All(value => !value))
            {
                return false;
            }

            set.Add(node);
            return true;

        }
    }
}
