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
public sealed record ConditionalJumpNode(CodeTreeValueNode ConditionEvaluation) : CodeTreeRoot
{
    public ConditionalJumpNode(CodeTreeRoot trueCase,
        CodeTreeRoot falseCase,
        CodeTreeValueNode conditionEvaluation) : this(conditionEvaluation)
    {
        TrueCase = trueCase;
        FalseCase = falseCase;
    }

    public CodeTreeRoot TrueCase { get; } = new SingleExitNode(Array.Empty<CodeTreeNode>());
    public CodeTreeRoot FalseCase { get; } = new SingleExitNode(Array.Empty<CodeTreeNode>());

    public override string ToString() =>
        $"If({ConditionEvaluation})\n" +
        "THEN\n" +
        TrueCase +
        "ELSE\n" +
        FalseCase;
}

/// <summary>
/// The root of a code tree for an operation that has a single exit point,
/// but has to be executed sequentially before the code in the following tree,
/// due to some side effect of the operation.
/// An example would be a code tree for an assignment operation, which is followed
/// by a code tree using the assigned value.
/// </summary>
/// <param name="NextTree"> Tree representing the code to be evaluated after this code tree </param>
/// <param name="Operations"> The operation to be performed in this code tree </param>
public sealed record SingleExitNode(IReadOnlyList<CodeTreeNode> Operations) : CodeTreeRoot
{
    public SingleExitNode(CodeTreeRoot? nextTree, CodeTreeNode operation) : this(nextTree, new[] { operation }) { }

    public SingleExitNode(CodeTreeRoot? nextTree, IReadOnlyList<CodeTreeNode> operations) : this(operations)
    {
        NextTree = nextTree;
    }

    public CodeTreeRoot? NextTree { get; set; }

    private readonly Lazy<string> _toString =
        new(() => (Operations.Count > 0 ? string.Join(',', Operations) : "[empty]") + '\n');
    public override string ToString() =>
        _toString.IsValueCreated ? $"Visited -> {_toString.Value}" : $"{_toString.Value}{NextTree}";
}
