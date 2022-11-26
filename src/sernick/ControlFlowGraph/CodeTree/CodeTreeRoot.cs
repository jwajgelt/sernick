namespace sernick.ControlFlowGraph.CodeTree;

public abstract record CodeTreeRoot : CodeTreeNode;

/// <summary>
/// The root of a code tree for an operation that conditionally jumps to
/// one of two different code paths, depending on the result of the
/// condition evaluation.
/// </summary>
/// <param name="TrueCase">The code tree to jump to if the condition is true</param>
/// <param name="FalseCase">The code tree to jump to if the condition is false</param>
/// <param name="ConditionEvaluation">Code tree for evaluating the condition</param>
public sealed record ConditionalJumpNode
(
    CodeTreeRoot TrueCase,
    CodeTreeRoot FalseCase,
    CodeTreeNode ConditionEvaluation
) : CodeTreeRoot;

/// <summary>
/// The root of a code tree for an operation that has a single exit point,
/// but has to be executed sequentially before the code in the following tree,
/// due to some side effect of the operation.
/// An example would be a code tree for an assignment operation, which is followed
/// by a code tree using the assigned value.
/// </summary>
/// <param name="NextTree"> Tree representing the code to be evaluated after this code tree </param>
/// <param name="Operation"> The operation to be performed in this code tree </param>
public sealed record SingleExitNode
    (CodeTreeRoot? NextTree, IReadOnlyList<CodeTreeNode> Operation) : CodeTreeRoot;
