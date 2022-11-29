namespace sernick.Ast.Analysis.ControlFlowGraph;

using System.Collections.Immutable;
using Nodes;
using sernick.ControlFlowGraph.CodeTree;
using Utility;

public static class FunctionCodeTreeMapGenerator
{
    public static IReadOnlyDictionary<FunctionDefinition, CodeTreeRoot> Process(AstNode root, Func<FunctionDefinition, CodeTreeRoot> unravel)
    {
        var visitor = new GeneratorVisitor(unravel);
        return root.Accept(visitor, Unit.I);
    }

    private sealed class GeneratorVisitor : AstVisitor<IImmutableDictionary<FunctionDefinition, CodeTreeRoot>, Unit>
    {
        private readonly Func<FunctionDefinition, CodeTreeRoot> _unravel;

        public GeneratorVisitor(Func<FunctionDefinition, CodeTreeRoot> unravel)
        {
            _unravel = unravel;
        }

        protected override IImmutableDictionary<FunctionDefinition, CodeTreeRoot> VisitAstNode(AstNode node, Unit param)
        {
            IImmutableDictionary<FunctionDefinition, CodeTreeRoot> emptyDict = ImmutableDictionary<FunctionDefinition, CodeTreeRoot>.Empty;
            return node.Children.Aggregate(emptyDict, (dict, child) =>
                dict.AddRange(child.Accept(this, Unit.I)));
        }

        public override IImmutableDictionary<FunctionDefinition, CodeTreeRoot> VisitFunctionDefinition(FunctionDefinition node, Unit param)
        {
            var result = VisitAstNode(node, Unit.I);
            return result.Add(node, _unravel(node));
        }
    }
}
