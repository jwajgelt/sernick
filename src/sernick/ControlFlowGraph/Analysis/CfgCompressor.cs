namespace sernick.ControlFlowGraph.Analysis;

using CodeTree;

public static class CfgCompressor
{
    /// <summary>
    /// Compresses paths of SingleExitNodes in CFG <paramref name="root"/> into single SingleExitNodes
    /// </summary>
    /// <returns>Compressed CFG</returns>
    public static CodeTreeRoot CompressPaths(CodeTreeRoot root)
    {
        var inDegree = new Dictionary<CodeTreeRoot, uint>(ReferenceEqualityComparer.Instance);
        Dfs(root, inDegree);

        var cache = new Dictionary<CodeTreeRoot, CodeTreeRoot>(ReferenceEqualityComparer.Instance);
        return CompressPath(root, inDegree, cache);
    }

    private static void Dfs(CodeTreeRoot root, IDictionary<CodeTreeRoot, uint> inDegree)
    {
        if (inDegree.TryGetValue(root, out var deg))
        {
            inDegree[root] = deg + 1u;
        }
        else
        {
            inDegree[root] = 1u;

            switch (root)
            {
                case SingleExitNode singleExit:
                    var nextNode = singleExit.NextTree;
                    if (nextNode is not null)
                    {
                        Dfs(nextNode, inDegree);
                    }

                    break;
                case ConditionalJumpNode conditionalJump:
                    Dfs(conditionalJump.TrueCase, inDegree);
                    Dfs(conditionalJump.FalseCase, inDegree);
                    break;
            }
        }
    }

    private static CodeTreeRoot CompressPath(CodeTreeRoot root,
        IReadOnlyDictionary<CodeTreeRoot, uint> inDegree,
        IDictionary<CodeTreeRoot, CodeTreeRoot> cache)
    {
        if (cache.TryGetValue(root, out var cached))
        {
            return cached;
        }

        switch (root)
        {
            case ConditionalJumpNode conditionalJumpRoot:
                // Assumption: no backward edge ends in a ConditionalJumpNode
                return cache[root] = new ConditionalJumpNode(
                    trueCase: CompressPath(conditionalJumpRoot.TrueCase, inDegree, cache),
                    falseCase: CompressPath(conditionalJumpRoot.FalseCase, inDegree, cache),
                    conditionalJumpRoot.ConditionEvaluation);
            case SingleExitNode singleExitRoot:
                var operations = singleExitRoot.Operations.ToList();
                var next = singleExitRoot.NextTree;

                while (next is not null && inDegree[next] == 1u && next is SingleExitNode singleExitNode)
                {
                    operations.AddRange(singleExitNode.Operations);
                    next = singleExitNode.NextTree;
                }

                var compressed = new SingleExitNode(null, operations);
                cache[root] = compressed;
                if (next is not null)
                {
                    compressed.NextTree = CompressPath(next, inDegree, cache);
                }

                return compressed;
            default:
                throw new ArgumentOutOfRangeException(nameof(root));
        }
    }
}
