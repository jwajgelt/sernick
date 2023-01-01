namespace sernick.Ast.Analysis.VariableInitialization;

using System.Collections.Immutable;
using Nodes;

public static class FunctionDefinitionHierarchyAnalysis
{
    public sealed record FunctionHierarchy(IReadOnlyDictionary<FunctionDefinition, FunctionDefinition?> EnclosingFunctionMap)
    {
        private FunctionDefinition? ParentOf(FunctionDefinition functionDefinition) => EnclosingFunctionMap[functionDefinition];

        public bool FunctionIsDescendantOf(FunctionDefinition functionDefinition, FunctionDefinition ancestor)
        {
            var parent = ParentOf(functionDefinition);
            while (parent != null)
            {
                if (parent == ancestor)
                {
                    return true;
                }

                parent = ParentOf(parent);
            }

            return false;
        }
    }

    public static FunctionHierarchy Process(FunctionDefinition main)
    {
        return new FunctionHierarchy(main.Accept(new FunctionHierarchyVisitor(), null));
    }

    private sealed class FunctionHierarchyVisitor
        : AstVisitor<ImmutableDictionary<FunctionDefinition, FunctionDefinition?>, FunctionDefinition?>
    {
        protected override ImmutableDictionary<FunctionDefinition, FunctionDefinition?> VisitAstNode(AstNode node, FunctionDefinition? param)
        {
            return node.Children
                .Select(child => child.Accept(this, param))
                .Aggregate(
                    ImmutableDictionary<FunctionDefinition, FunctionDefinition?>.Empty,
                    (acc, result) => acc.AddRange(result));
        }

        public override ImmutableDictionary<FunctionDefinition, FunctionDefinition?> VisitFunctionDefinition(FunctionDefinition node, FunctionDefinition? param)
        {
            return VisitAstNode(node, node).Add(node, param);
        }
    }
}
