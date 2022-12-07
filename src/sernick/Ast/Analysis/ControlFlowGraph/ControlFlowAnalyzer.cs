namespace sernick.Ast.Analysis.ControlFlowGraph;

using Compiler.Function;
using FunctionContextMap;
using NameResolution;
using Nodes;
using sernick.Ast.Analysis.TypeChecking;
using sernick.ControlFlowGraph.CodeTree;
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
        TypeCheckingResult typeCheckingResult,
        Func<AstNode, NameResolutionResult, IFunctionContext, FunctionContextMap, IReadOnlyList<SingleExitNode>>
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
                (root, next, resultVariable) =>
                {
                    var nodes = pullOutSideEffects(root, nameResolution, currentFunctionContext, contextMap);
                    if (nodes.Count == 0)
                    {
                        return next;
                    }

                    foreach (var (node, nextNode) in nodes.Zip(nodes.Skip(1)))
                    {
                        node.NextTree = nextNode;
                    }

                    if (resultVariable is not null && nodes[^1].Operations[^1] is CodeTreeValueNode valueNode)
                    {
                        nodes[^1].NextTree = new SingleExitNode(next,
                            new[] { currentFunctionContext.GenerateVariableWrite(resultVariable, valueNode) });
                    }

                    return nodes[0];
                },
                variableFactory,
                typeCheckingResult
            );

        var resultVariable = variableFactory.NewVariable();

        var prologue = currentFunctionContext.GeneratePrologue();
        var epilogue =
            currentFunctionContext.GenerateEpilogue(currentFunctionContext.GenerateVariableRead(resultVariable));

        prologue[^1].NextTree = functionDefinition.Body.Accept(visitor,
            new ControlFlowVisitorParam(
                epilogue[0],
                null,
                null,
                epilogue[0],
                resultVariable
            ));

        return prologue[0];
    }

    private sealed record ControlFlowVisitorParam
    (
        CodeTreeRoot Next, // CFG node that will be visited after the CFG for the currently processed AST 
        CodeTreeRoot? Break, // CFG node that will be visited after a break statement
        CodeTreeRoot? Continue, // CFG node that will be visited after a continue statement
        CodeTreeRoot Return, // CFG node that will be visited after a return statement
        IFunctionVariable? ResultVariable // variable in which the result should be stored
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
            _functionContext.AddLocal(temp, false);
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

        public ControlFlowVisitor
        (
            IFunctionContext currentFunctionContext,
            IReadOnlySet<AstNode> nodesWithControlFlow,
            Func<AstNode, CodeTreeRoot, IFunctionVariable?, CodeTreeRoot> pullOutSideEffects,
            TemporaryLocalVariableFactory variableFactory,
            TypeCheckingResult typeChecking
        )
        {
            _pullOutSideEffects = pullOutSideEffects;
            _currentFunctionContext = currentFunctionContext;
            _nodesWithControlFlow = nodesWithControlFlow;
            _variableFactory = variableFactory;
            _typeChecking = typeChecking;
        }

        protected override CodeTreeRoot VisitAstNode(AstNode node, ControlFlowVisitorParam param)
        {
            if (!_nodesWithControlFlow.Contains(node))
            {
                return _pullOutSideEffects(node, param.Next, param.ResultVariable);
            }

            var result = param.Next;
            var nextParam = param;
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
            result.NextTree = node.Inner.Accept(this, param with { Next = result, Break = param.Next, Continue = result });
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
                ResultVariable = tempVariable
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
            return node.ReturnValue?.Accept(this, param with { Next = param.Return }) ?? param.Return;
        }

        public override CodeTreeRoot VisitInfix(Infix node, ControlFlowVisitorParam param)
        {
            if (node.Operator is not (Infix.Op.ScAnd or Infix.Op.ScOr))
            {
                var (leftVariable, leftVariableValueNode) = GenerateTemporaryAst(node.Left);
                var (rightVariable, rightVariableValueNode) = GenerateTemporaryAst(node.Right);
                var infix = node with
                {
                    Left = leftVariableValueNode,
                    Right = rightVariableValueNode
                };
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

            var leftTempVariable = _variableFactory.NewVariable();
            var rightTempVariable = _variableFactory.NewVariable();
            var evaluateRight = node.Right.Accept(this,
                param with
                {
                    Next = param.ResultVariable is not null
                        ? new SingleExitNode(param.Next, new[]
                        {
                            _currentFunctionContext.GenerateVariableWrite(param.ResultVariable,
                                _currentFunctionContext.GenerateVariableRead(rightTempVariable))
                        })
                        : param.Next,
                    ResultVariable = rightTempVariable
                });
            var returnLeft = param.ResultVariable is not null
                ? new SingleExitNode(param.Next, new[]
                {
                    _currentFunctionContext.GenerateVariableWrite(param.ResultVariable,
                        _currentFunctionContext.GenerateVariableRead(leftTempVariable))
                })
                : param.Next;

            return node.Left.Accept(this, param with
            {
                Next = new ConditionalJumpNode(
                    node.Operator is Infix.Op.ScAnd ? evaluateRight : returnLeft,
                    node.Operator is Infix.Op.ScAnd ? returnLeft : evaluateRight,
                    _currentFunctionContext.GenerateVariableRead(leftTempVariable)
                ),
                ResultVariable = leftTempVariable
            });

        }

        public override CodeTreeRoot VisitFunctionCall(FunctionCall node, ControlFlowVisitorParam param)
        {
            var last = new SingleExitNode(null, Array.Empty<CodeTreeNode>());
            CodeTreeRoot result = last;
            var arguments = node.Arguments.Reverse().Select(argumentNode =>
            {
                var (tempVariable, variableValueNode) = GenerateTemporaryAst(argumentNode);
                result = argumentNode.Accept(this, param with { Next = result, ResultVariable = tempVariable });
                return variableValueNode;
            }).Reverse();

            var functionCall = node with { Arguments = arguments.ToList() };
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

            var (tempVariable, variableValueNode) = GenerateTemporaryAst(node.InitValue);

            var variableDeclaration = node with { InitValue = variableValueNode };
            return node.InitValue.Accept(this, param with { ResultVariable = tempVariable, Next = _pullOutSideEffects(variableDeclaration, param.Next, param.ResultVariable) });
        }

        public override CodeTreeRoot VisitAssignment(Assignment node, ControlFlowVisitorParam param)
        {
            if (!_nodesWithControlFlow.Contains(node))
            {
                return _pullOutSideEffects(node, param.Next, param.ResultVariable);
            }

            var (tempVariable, variableValueNode) = GenerateTemporaryAst(node.Right);

            var assignment = node with { Right = variableValueNode };
            return node.Right.Accept(this, param with { ResultVariable = tempVariable, Next = _pullOutSideEffects(assignment, param.Next, param.ResultVariable) });
        }

        public override CodeTreeRoot VisitIdentifier(Identifier node, ControlFlowVisitorParam param)
        {
            throw new NotSupportedException("Control flow analysis shouldn't descend into Identifiers");
        }

        public override CodeTreeRoot VisitFunctionParameterDeclaration(FunctionParameterDeclaration node, ControlFlowVisitorParam param)
        {
            throw new NotSupportedException("Control flow analysis shouldn't descend into function parameter declaration");
        }

        protected override CodeTreeRoot VisitSimpleValue(SimpleValue node, ControlFlowVisitorParam param)
        {
            throw new NotSupportedException("Control flow analysis shouldn't descend into simple values");
        }

        protected override CodeTreeRoot VisitLiteralValue(LiteralValue node, ControlFlowVisitorParam param)
        {
            throw new NotSupportedException("Control flow analysis shouldn't descend into literal values");
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
            _currentFunctionContext.AddLocal(tempVariable, false);
            var variableValue = new VariableValue(identifier, node.LocationRange);
            return (tempVariable, variableValue);
        }
    }

    private sealed class ContainsControlFlowVisitor : AstVisitor<bool, ISet<AstNode>>
    {
        protected override bool VisitAstNode(AstNode node, ISet<AstNode> set)
        {
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
