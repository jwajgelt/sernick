using System;
using sernick.CodeGeneration;
using sernick.ControlFlowGraph.CodeTree;

namespace sernick.ControlFlowGraph.Analysis;

public interface IInstructionCovering
{
    public IEnumerable<IInstruction> Cover(SingleExitNode node, Label? next);
    public IEnumerable<IInstruction> Cover(ConditionalJumpNode node, Label trueCase, Label falseCase);

}

