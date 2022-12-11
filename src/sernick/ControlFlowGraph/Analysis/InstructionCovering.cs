namespace sernick.ControlFlowGraph.Analysis;

using CodeGeneration;
using CodeGeneration.InstructionSelection;
using static sernick.CodeGeneration.InstructionSelection.CodeTreePatternRuleMatchExtensions;
using CodeTree;

public sealed class InstructionCovering
{
    record TreeCoverResult(int cost, IEnumerable<CodeTreeNode>? leaves, GenerateInstructions? generator);
    IEnumerable<CodeTreePatternRule> _rules;

    Dictionary<CodeTreeNode, TreeCoverResult?> _resMemoizer;
    public InstructionCovering(IEnumerable<CodeTreePatternRule> rules)
    {
        _rules = rules;
        _resMemoizer = new Dictionary<CodeTreeNode, TreeCoverResult?>(ReferenceEqualityComparer.Instance);    
    }

    private TreeCoverResult? CoverTree(CodeTreeNode node)
    {
        if(_resMemoizer.TryGetValue(node, out TreeCoverResult? result))
        {
            return result;
        }

        TreeCoverResult? best = null; 
        foreach(CodeTreePatternRule patternRule in _rules)
        {
            if(patternRule.TryMatchCodeTreeNode(node,
                out IEnumerable<CodeTreeNode>? leaves,
                out GenerateInstructions? generateInstructions
                ))
            {
                int? cost = 1 + LeavesCost(leaves);
                if(cost != null && (best == null || cost < best.cost))
                { 
                    best = new TreeCoverResult(cost.GetValueOrDefault(), leaves, generateInstructions);
                }
            }
        }

        _resMemoizer[node] = best;
        return best;
    }

    public IEnumerable<IInstruction> Cover(SingleExitNode node, Label next)
    {
        var result = CoverTree(node);
        if(result is null)
        {
            throw new Exception("Unable to cover with given covering rules set.");
        }
        return GenerateSingleExitCovering(result, next);
    }

    public IEnumerable<IInstruction> Cover(ConditionalJumpNode node, Label trueCase, Label falseCase)
    {
        var result = CoverTree(node);
        if(result is null)
        {
            throw new Exception("Unable to cover with given covering rules set.");
        }
        return GenerateConditionalJumpCovering(result, trueCase, falseCase);
    }

    private int? LeavesCost(IEnumerable<CodeTreeNode>? leaves)
    {
        int cost = 0;
        if (leaves is not null)
        {
            foreach (CodeTreeNode leaf in leaves)
            {
                var leafCover = CoverTree(leaf);
                if(leafCover is null)
                {
                    return null;
                }
                cost += leafCover.cost;
            }
        }

        return cost;
    }

    private IEnumerable<IInstruction> GenerateCovering(TreeCoverResult result)
    {
        throw new NotImplementedException();
    }

    private IEnumerable<IInstruction> GenerateSingleExitCovering(TreeCoverResult result, Label next)
    {
        throw new NotImplementedException();
    }

    private IEnumerable<IInstruction> GenerateConditionalJumpCovering(TreeCoverResult result, Label trueCase, Label falseCase)
    {
        throw new NotImplementedException();
    }
}
