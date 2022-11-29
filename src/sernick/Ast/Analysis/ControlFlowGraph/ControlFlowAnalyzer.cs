namespace sernick.Ast.Analysis.ControlFlowGraph;

using Compiler.Function;
using FunctionContextMap;
using NameResolution;
using Nodes;
using sernick.ControlFlowGraph.CodeTree;
using FunctionCall = Nodes.FunctionCall;

public static class ControlFlowAnalyzer
{
    public static CodeTreeRoot UnravelControlFlow(
        AstNode root,
        NameResolutionResult nameResolution,
        FunctionContextMap contextMap,
        Func<AstNode, NameResolutionResult, IFunctionContext, IReadOnlyList<SingleExitNode>> pullOutSideEffects)
    {
        var visitor = new ControlFlowVisitor(nameResolution, currentFunctionContext, pullOutSideEffects);
        return visitor.VisitAstTree(root, );
    }

    private sealed record ControlFlowVisitorParam
    (
        CodeTreeRoot? Next,
        CodeTreeRoot? Break,
        CodeTreeRoot? Continue,
        CodeTreeRoot Return,
        IFunctionVariable Result
    );

    private interface ILocalFunctionVariableFactory
    {
        IFunctionVariable NewIFunctionVariable();
    }

    private sealed class ControlFlowVisitor : AstVisitor<CodeTreeRoot, ControlFlowVisitorParam>
    {
        private readonly Func<AstNode, CodeTreeRoot?, CodeTreeRoot>
            _pullOutSideEffects;

        private readonly NameResolutionResult _nameResolution;
        private readonly IFunctionContext _currentFunctionContext;
        private readonly Dictionary<AstNode, bool> _containsControlFlow;
        private readonly FunctionContextMap _functionContextMap;
        private readonly ILocalFunctionVariableFactory _functionVariableFactory;

        public ControlFlowVisitor
        (
            NameResolutionResult nameResolution,
            IFunctionContext currentFunctionContext,
            Dictionary<AstNode, bool> containsControlFlow,
            Func<AstNode, CodeTreeRoot?, CodeTreeRoot> pullOutSideEffects
        )
        {
            _pullOutSideEffects = pullOutSideEffects;
            _nameResolution = nameResolution;
            _currentFunctionContext = currentFunctionContext;
            _containsControlFlow = containsControlFlow;
        }

        protected override CodeTreeRoot VisitAstNode(AstNode node, ControlFlowVisitorParam param) //TODO: how does Result work in this case??
        {
            if (!_containsControlFlow[node])
            {
                return _pullOutSideEffects(node, param.Next);
            }

            var result = param.Next;
            var nextParam = param;
            foreach (var currentNode in node.Children.Reverse())
            {
                result = currentNode.Accept(this, nextParam);
                nextParam = nextParam with { Next = result };
            }

            return result;
        }

        public override CodeTreeRoot VisitLoopStatement(LoopStatement node, ControlFlowVisitorParam param) //TODO: what does loop return? is it always Unit???
        {
            var x = new SingleExitNode(null, Array.Empty<CodeTreeNode>());
            x.NextTree = node.Inner.Accept(this, param with { Next = x, Break = param.Next, Continue = x });
            return x;
        }

        public override CodeTreeRoot VisitIfStatement(IfStatement node, ControlFlowVisitorParam param)
        {
            var tempVariable = _functionVariableFactory.NewIFunctionVariable();

            return node.Condition.Accept(this, param with
            {
                Next = new ConditionalJumpNode(
                    node.IfBlock.Accept(this, param),
                    node.ElseBlock is null ? param.Next! : node.ElseBlock.Accept(this, param),
                    _currentFunctionContext.GenerateVariableRead(tempVariable)
                ),
                Result = tempVariable
            });
        }

        public override CodeTreeRoot VisitBreakStatement(BreakStatement node, ControlFlowVisitorParam param)
        {
            return param.Break!;
        }

        public override CodeTreeRoot VisitContinueStatement(ContinueStatement node, ControlFlowVisitorParam param)
        {
            return param.Continue!;
        }

        public override CodeTreeRoot VisitReturnStatement(ReturnStatement node, ControlFlowVisitorParam param)
        {
            return node.ReturnValue is not null ? node.ReturnValue.Accept(this, param with { Next = param.Return }) : param.Return;
        }

        public override CodeTreeRoot VisitCodeBlock(CodeBlock node, ControlFlowVisitorParam param)
        {
            return node.Inner.Accept(this, param);
        }

        public override CodeTreeRoot VisitInfix(Infix node, ControlFlowVisitorParam param)
        {
            if (node.Operator is not (Infix.Op.ScAnd or Infix.Op.ScOr))
            {
                return VisitAstNode(node, param);
            }

            var leftTempVariable = _functionVariableFactory.NewIFunctionVariable();
            var rightTempVariable = _functionVariableFactory.NewIFunctionVariable();
            var evaluateRight = node.Right.Accept(this,
                param with
                {
                    Next = new SingleExitNode(param.Next, new[]{
                        _currentFunctionContext.GenerateVariableWrite(param.Result,
                            _currentFunctionContext.GenerateVariableRead(rightTempVariable))}),
                    Result = rightTempVariable
                });
            var returnLeft = new SingleExitNode(param.Next, new[]{
                _currentFunctionContext.GenerateVariableWrite(param.Result,
                    _currentFunctionContext.GenerateVariableRead(leftTempVariable))});

            return node.Left.Accept(this, param with
            {
                Next = new ConditionalJumpNode(
                    node.Operator is Infix.Op.ScAnd ? evaluateRight : returnLeft,
                    node.Operator is Infix.Op.ScAnd ? returnLeft : evaluateRight,
                    _currentFunctionContext.GenerateVariableRead(leftTempVariable)
                ),
                Result = leftTempVariable
            });

        }

        public override CodeTreeRoot VisitFunctionCall(FunctionCall node, ControlFlowVisitorParam param)
        {
            var functionContext = _functionContextMap[_nameResolution.CalledFunctionDeclarations[node]];
            CodeTreeRoot result, next = null;

            var arguments = node.Arguments.Reverse().Select(argument =>
            {
                var tempVariable = _functionVariableFactory.NewIFunctionVariable();

                result = argument.Accept(this, param with { Next = next, Result = tempVariable });

                return _currentFunctionContext.GenerateVariableRead(tempVariable);
            }).Reverse();
            functionContext.GenerateCall(arguments).CodeGraph;//TODO: arguments first or prologue???
        }

        public override CodeTreeRoot VisitFunctionDefinition(FunctionDefinition node, ControlFlowVisitorParam param)
        {
            return null;
        }

        public override CodeTreeRoot VisitIdentifier(Identifier node, ControlFlowVisitorParam param)
        {
            throw new NotSupportedException("AAAA");
        }
    }
}
