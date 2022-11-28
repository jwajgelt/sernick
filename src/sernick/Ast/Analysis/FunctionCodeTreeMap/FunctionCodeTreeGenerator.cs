namespace sernick.Ast.Analysis.FunctionCodeTreeMap;

using ControlFlowGraph;
using Nodes;
using sernick.ControlFlowGraph.CodeTree;
using Utility;
using FunctionCall = Nodes.FunctionCall;

public class FunctionCodeTreeMapGenerator
{
    public static IDictionary<FunctionDefinition, CodeTreeRoot> Process(AstNode root, Func<FunctionDefinition, CodeTreeRoot> unravel)
    {
        var visitor = new GeneratorVisitor(unravel);
        return root.Accept(visitor, Unit.I);
    }
    
    private sealed class GeneratorVisitor : AstVisitor<IDictionary<FunctionDefinition, CodeTreeRoot>, Unit>
    {
        private readonly Func<FunctionDefinition, CodeTreeRoot> _unravel;
        
        public GeneratorVisitor(Func<FunctionDefinition, CodeTreeRoot> unravel)
        {
            _unravel = unravel;
        }
        protected override IDictionary<FunctionDefinition, CodeTreeRoot> VisitAstNode(AstNode node, Unit param)
        {
            IDictionary<FunctionDefinition, CodeTreeRoot> emptyDict = new Dictionary<FunctionDefinition, CodeTreeRoot>();
            return node.Children.Aggregate(emptyDict, (dict, child) =>
                Merge(dict, child.Accept(this, Unit.I)));
        }

        public override IDictionary<FunctionDefinition, CodeTreeRoot> VisitFunctionDefinition(FunctionDefinition node, Unit param)
        {
            var result = VisitAstNode(node, Unit.I);
            result[node] = _unravel(node);
            return result;
        }
    }

    /// <summary>
    /// Adds all elements of the smaller dict to the other
    /// </summary>
    private static IDictionary<K, V> Merge<K, V>(IDictionary<K, V> dict, IDictionary<K, V> other)
    {
        return dict.Count > other.Count ? dict.MergeWith(other) : other.MergeWith(dict);
    }
}
