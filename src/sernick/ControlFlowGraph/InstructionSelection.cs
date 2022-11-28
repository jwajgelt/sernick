namespace sernick.ControlFlowGraph;

using CodeTree;

public sealed class InstructionMatcher
{
    private readonly IReadOnlyCollection<CodeTreePattern> _patterns;

    public InstructionMatcher(IEnumerable<CodeTreePattern> patterns) => _patterns = patterns.ToList();

    public bool MatchCodeTree(CodeTreeNode root, out IEnumerable<CodeTreeNode> leaves)
    {
        foreach (var pattern in _patterns)
        {
            if (pattern.TryMatch(root, out var matchedLeaves))
            {
                leaves = matchedLeaves;
                return true;
            }
        }

        // shouldn't happen (perhaps only in tests)
        leaves = Enumerable.Empty<CodeTreeNode>();
        return false;
    }
}

public static class CodeTreePatternPredicates
{
    public static Predicate<T> Any<T>() => _ => true;
    public static Predicate<T> Is<T>(T expected) => given => Equals(given, expected);
    public static Predicate<RegisterValue> IsZero => node => node.Value == 0;
}

public abstract record CodeTreePattern
{
    /// <summary>
    /// Tries to match itself onto <see cref="root"/>. If successful,
    /// returns subtrees in <see cref="root"/> that were matched onto <see cref="WildcardNode"/>s of this pattern.
    /// </summary>
    public abstract bool TryMatch(CodeTreeNode root, out IEnumerable<CodeTreeValueNode> leaves);

    /// <summary>
    /// <see cref="BinaryOperationNode"/> pattern,
    /// allowing to filter binary-operator of the matching node via <see cref="operation"/> predicate.
    /// </summary>
    public static CodeTreePattern BinaryOperationNode(
        Predicate<BinaryOperation> operation,
        CodeTreePattern left,
        CodeTreePattern right) => new BinaryOperationNodePattern(operation, left, right);

    /// <summary>
    /// <see cref="UnaryOperationNode"/> pattern,
    /// allowing to filter unary-operator of the matching node via <see cref="operation"/> predicate.
    /// </summary>
    public static CodeTreePattern UnaryOperationNode(
        Predicate<UnaryOperation> operation,
        CodeTreePattern operand) => new UnaryOperationNodePattern(operation, operand);

    /// <summary>
    /// <see cref="Constant"/> pattern,
    /// allowing to filter constant value of the matching node via <see cref="value"/> predicate.
    /// </summary>
    public static CodeTreePattern Constant(Predicate<RegisterValue> value) => new ConstantPattern(value);

    /// <summary>
    /// <see cref="RegisterRead"/> pattern,
    /// allowing to filter register of the matching node via <see cref="Register"/> predicate.
    /// </summary>
    public static CodeTreePattern RegisterRead(Predicate<Register> register) => new RegisterReadPattern(register);

    /// <summary>
    /// <see cref="RegisterWrite"/> pattern,
    /// allowing to filter register of the matching node via <see cref="Register"/> predicate.
    /// </summary>
    public static CodeTreePattern RegisterWrite(
        Predicate<Register> register,
        CodeTreePattern value) => new RegisterWritePattern(register, value);

    /// <summary>
    /// <see cref="MemoryRead"/> pattern.
    /// </summary>
    public static CodeTreePattern MemoryRead(CodeTreePattern location) => new MemoryReadPattern(location);

    /// <summary>
    /// <see cref="MemoryWrite"/> pattern.
    /// </summary>
    public static CodeTreePattern MemoryWrite(
        CodeTreePattern location,
        CodeTreePattern value) => new MemoryWritePattern(location, value);

    /// <summary>
    /// Wildcard pattern, which matches any <see cref="CodeTreeValueNode"/>.
    /// </summary>
    public static CodeTreePattern WildcardNode => new WildcardNodePattern();

    private sealed record BinaryOperationNodePattern(Predicate<BinaryOperation> Operation, CodeTreePattern Left,
        CodeTreePattern Right) : CodeTreePattern
    {
        public override bool TryMatch(CodeTreeNode root, out IEnumerable<CodeTreeValueNode> leaves)
        {
            leaves = Enumerable.Empty<CodeTreeValueNode>();
            return root is BinaryOperationNode node &&
                   Operation.Invoke(node.Operation) &&
                   Left.TryMatch(node.Left, out var leftLeaves) &&
                   Right.TryMatch(node.Right, out var rightLeaves) &&
                   Run(leaves = leftLeaves.Concat(rightLeaves));
        }
    }

    private sealed record UnaryOperationNodePattern(Predicate<UnaryOperation> Operation, CodeTreePattern Operand) : CodeTreePattern
    {
        public override bool TryMatch(CodeTreeNode root, out IEnumerable<CodeTreeValueNode> leaves)
        {
            leaves = Enumerable.Empty<CodeTreeValueNode>();
            return root is UnaryOperationNode node &&
                   Operation.Invoke(node.Operation) &&
                   Operand.TryMatch(node.Operand, out leaves);
        }
    }

    private sealed record ConstantPattern(Predicate<RegisterValue> Value) : CodeTreePattern
    {
        public override bool TryMatch(CodeTreeNode root, out IEnumerable<CodeTreeValueNode> leaves)
        {
            leaves = Enumerable.Empty<CodeTreeValueNode>();
            return root is Constant node &&
                   Value.Invoke(node.Value);
        }
    }

    private sealed record RegisterReadPattern(Predicate<Register> Register) : CodeTreePattern
    {
        public override bool TryMatch(CodeTreeNode root, out IEnumerable<CodeTreeValueNode> leaves)
        {
            leaves = Enumerable.Empty<CodeTreeValueNode>();
            return root is RegisterRead node &&
                   Register.Invoke(node.Register);
        }
    }

    private sealed record RegisterWritePattern(Predicate<Register> Register, CodeTreePattern Value) : CodeTreePattern
    {
        public override bool TryMatch(CodeTreeNode root, out IEnumerable<CodeTreeValueNode> leaves)
        {
            leaves = Enumerable.Empty<CodeTreeValueNode>();
            return root is RegisterWrite node &&
                   Register.Invoke(node.Register) &&
                   Value.TryMatch(node.Value, out leaves);
        }
    }

    private sealed record MemoryReadPattern(CodeTreePattern MemoryLocation) : CodeTreePattern
    {
        public override bool TryMatch(CodeTreeNode root, out IEnumerable<CodeTreeValueNode> leaves)
        {
            leaves = Enumerable.Empty<CodeTreeValueNode>();
            return root is MemoryRead node &&
                   MemoryLocation.TryMatch(node.MemoryLocation, out leaves);
        }
    }

    private sealed record MemoryWritePattern(CodeTreePattern MemoryLocation, CodeTreePattern Value) : CodeTreePattern
    {
        public override bool TryMatch(CodeTreeNode root, out IEnumerable<CodeTreeValueNode> leaves)
        {
            leaves = Enumerable.Empty<CodeTreeValueNode>();
            return root is MemoryWrite node &&
                   MemoryLocation.TryMatch(node.MemoryLocation, out var leavesLocation) &&
                   Value.TryMatch(node.Value, out var leavesValue) &&
                   Run(leaves = leavesLocation.Concat(leavesValue));
        }
    }

    private sealed record WildcardNodePattern : CodeTreePattern
    {
        public override bool TryMatch(CodeTreeNode root, out IEnumerable<CodeTreeValueNode> leaves)
        {
            if (root is not CodeTreeValueNode rootValue)
            {
                leaves = Enumerable.Empty<CodeTreeValueNode>();
                return false;
            }

            leaves = Enumerable.Repeat(rootValue, 1);
            return true;
        }
    }

    // hack method, which allows to treat assignments as "true" values
    private static bool Run<T>(T assignment) => assignment is not null;
}
