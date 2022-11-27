namespace sernick.Ast.Analysis.ControlFlowGraph;

using Compiler.Function;
using NameResolution;
using Nodes;
using sernick.ControlFlowGraph.CodeTree;
using Utility;
using FunctionCall = Nodes.FunctionCall;
using SideEffectsVisitorParam = Utility.Unit;
using Variable = Nodes.VariableDeclaration;

public static class SideEffectsAnalyzer
{
    public static IReadOnlyList<CodeTreeRoot> PullOutSideEffects(AstNode root, NameResolutionResult nameResolution, IFunctionContext currentFunctionContext)
    {
        var visitor = new SideEffectsVisitor(nameResolution, currentFunctionContext);
        var visitorResult = root.Accept(visitor, Unit.I);
        return visitorResult.Select(tree => new SingleExitNode(null, tree.CodeTreeRootChildren)).ToList();
    }

    private record TreeWithEffects
    (
        HashSet<Variable> ReadVariables,
        HashSet<Variable> WrittenVariables,
        bool CallsFunction,
        List<CodeTreeNode> CodeTreeRootChildren
    )
    {
        // we can merge two trees if
        // 1. neither is a function call, since these can be effectful
        // 2. no variable read in `next` was written in `this`, or vice versa
        // 3. no variable was written in both
        public bool CanMergeWith(TreeWithEffects next) => !CallsFunction && !next.CallsFunction
            && !next.ReadVariables.Any(variable => WrittenVariables.Contains(variable))
            && !ReadVariables.Any(variable => next.WrittenVariables.Contains(variable))
            && !WrittenVariables.Any(variable => next.WrittenVariables.Contains(variable));

        public void MergeWith(TreeWithEffects other)
        {
            ReadVariables.UnionWith(other.ReadVariables);
            WrittenVariables.UnionWith(other.WrittenVariables);
            CodeTreeRootChildren.AddRange(other.CodeTreeRootChildren);
        }
    }

    private static void AddTreesMerging(this List<TreeWithEffects> left, ICollection<TreeWithEffects> right)
    {
        if (!right.Any())
        {
            return;
        }

        if (left.Count == 0 || !left[^1].CanMergeWith(right.First()))
        {
            left.AddRange(right);
            return;
        }

        left[^1].MergeWith(right.First());
        left.AddRange(right.Skip(1));
    }

    private class SideEffectsVisitor : AstVisitor<List<TreeWithEffects>, SideEffectsVisitorParam>
    {
        public SideEffectsVisitor(NameResolutionResult nameResolution, IFunctionContext currentFunctionContext)
        {
            _nameResolution = nameResolution;
            _currentFunctionContext = currentFunctionContext;
        }

        protected override List<TreeWithEffects> VisitAstNode(AstNode node, SideEffectsVisitorParam param)
        {
            var results = node.Children.Select(child => child.Accept(this, param));
            var treeList = new List<TreeWithEffects>();

            foreach (var result in results)
            {
                if (result.Any())
                {
                    continue;
                }

                if (treeList.Count == 0)
                {
                    treeList.AddRange(result);
                    continue;
                }

                var prev = treeList[^1];
                var next = result.First();

                // If no side effects from the last tree so far
                // impacts the next tree to be added, merge the two trees.
                // Since the tree after `next` in result wasn't merged with `next`
                // before, we don't need to check if we can merge it now, and can just
                // add the rest of `result` to the tree list
                if (prev.CanMergeWith(next))
                {
                    prev.MergeWith(next);
                }
                else
                {
                    treeList.Add(prev);
                }

                treeList.AddRange(result.Skip(1));
            }

            return treeList;
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

        public override List<TreeWithEffects> VisitFunctionCall(FunctionCall node, SideEffectsVisitorParam param)
        {
            throw new NotSupportedException("Side effects analysis shouldn't descend into function calls");
        }

        public override List<TreeWithEffects> VisitInfix(Infix node, SideEffectsVisitorParam param)
        {
            var leftResult = node.Left.Accept(this, param);
            var rightResult = node.Right.Accept(this, param);
            var leftValue = leftResult[^1].CodeTreeRootChildren[^1];
            var rightValue = rightResult[^1].CodeTreeRootChildren[^1];
            var operationResult = node.Operator switch
            {
                Infix.Op.Plus => new BinaryOperationNode(BinaryOperation.Add, leftValue, rightValue),
                Infix.Op.Minus => new BinaryOperationNode(BinaryOperation.Sub, leftValue, rightValue),
                Infix.Op.Less => new BinaryOperationNode(BinaryOperation.LessThan, leftValue, rightValue),
                Infix.Op.Greater => new BinaryOperationNode(BinaryOperation.GreaterThan, leftValue, rightValue),
                Infix.Op.LessOrEquals => new BinaryOperationNode(BinaryOperation.LessThanEqual, leftValue, rightValue),
                Infix.Op.GreaterOrEquals => new BinaryOperationNode(BinaryOperation.GreaterThanEqual, leftValue, rightValue),
                Infix.Op.Equals => new BinaryOperationNode(BinaryOperation.Equal, leftValue, rightValue),
                Infix.Op.ScAnd => throw new NotSupportedException(
                    "Side effects analysis expects an AST with linear flow"),
                Infix.Op.ScOr => throw new NotSupportedException(
                    "Side effects analysis expects an AST with linear flow"),
                _ => throw new ArgumentOutOfRangeException()
            };
            // this is overly conservative. Can we track read variables for each code tree root child?
            var readVariables = new HashSet<VariableDeclaration>();
            readVariables.UnionWith(leftResult[^1].ReadVariables);
            readVariables.UnionWith(rightResult[^1].ReadVariables);
            var writtenVariables = new HashSet<VariableDeclaration>();

            var operationTree = new TreeWithEffects
            (
                readVariables,
                writtenVariables,
                false,
                new List<CodeTreeNode> { operationResult }
            );

            var result = new List<TreeWithEffects>(leftResult.SkipLast(1));
            result.AddTreesMerging(rightResult.SkipLast(1).ToArray());
            result.AddTreesMerging(new[] { operationTree });
            return result;
        }

        public override List<TreeWithEffects> VisitAssignment(Assignment node, SideEffectsVisitorParam param)
        {
            var variable = _nameResolution.AssignedVariableDeclarations[node];
            return GenerateVariableAssignmentTree(variable, node.Right, param);
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
                    false,
                    new List<CodeTreeNode> { variableReadTree }
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
                    false,
                    new List<CodeTreeNode> { new Constant(new RegisterValue(node.Value ? 1 : 0)) }
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
                    false,
                    new List<CodeTreeNode> { new Constant(new RegisterValue(node.Value)) }
                )
            };
        }

        public override List<TreeWithEffects> VisitEmptyExpression(EmptyExpression node, SideEffectsVisitorParam param)
        {
            return new List<TreeWithEffects>();
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
            last.WrittenVariables.Add(variable);
            last.CodeTreeRootChildren[^1] = _currentFunctionContext.GenerateVariableWrite(variable, last.CodeTreeRootChildren[^1]);
            return result;
        }

        private readonly NameResolutionResult _nameResolution;
        private readonly IFunctionContext _currentFunctionContext;
    }
}
