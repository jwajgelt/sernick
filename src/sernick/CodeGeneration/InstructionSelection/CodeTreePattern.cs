namespace sernick.CodeGeneration.InstructionSelection;

using System.Diagnostics.CodeAnalysis;
using Compiler.Function;
using ControlFlowGraph.CodeTree;
using Utility;

public static class CodeTreePatternPredicates
{
    public static Predicate<T> Any<T>() => _ => true;
    public static Predicate<T> Is<T>(T expected) => given => Equals(given, expected);
    public static Predicate<RegisterValue> IsZero => node => node.Value == 0;

    public static Predicate<T> IsAnyOf<T>(params T[] expected) => expected.Contains;
}

public abstract record CodeTreePatternBase;

public sealed record SingleExitNodePattern : CodeTreePatternBase
{
    // Linter forces this to be static, because it doesn't use instance data xd
    public static bool TryMatch(SingleExitNode node, out IEnumerable<CodeTreeNode> leaves)
    {
        leaves = node.Operations;
        return true;
    }
}

public sealed record ConditionalJumpNodePattern : CodeTreePatternBase
{
    public static bool TryMatch(ConditionalJumpNode node, out IEnumerable<CodeTreeNode> leaves)
    {
        leaves = node.ConditionEvaluation.Enumerate();
        return true;
    }
}

public abstract record CodeTreePattern : CodeTreePatternBase
{
    /// <summary>
    /// Tries to match itself onto <see cref="root"/>. If successful,
    /// returns subtrees in <see cref="root"/> that were matched onto <see cref="WildcardNode"/>s of this pattern.
    /// </summary>
    /// <param name="values">Map of values matched in all non-wildcard nodes. Can be one of
    /// <list type="number">
    /// <item><see cref="RegisterValue"/></item>
    /// <item><see cref="Register"/></item>
    /// <item><see cref="IFunctionCaller"/></item>
    /// <item><see cref="BinaryOperation"/></item>
    /// <item><see cref="UnaryOperation"/></item>
    /// </list>
    /// </param>
    public abstract bool TryMatch(CodeTreeNode root,
        [NotNullWhen(true)] out IEnumerable<CodeTreeValueNode>? leaves,
        IDictionary<CodeTreePattern, object> values);

    /// <summary>
    /// <see cref="BinaryOperationNode"/> pattern,
    /// allowing to filter binary-operator of the matching node via <see cref="operation"/> predicate.
    /// </summary>
    /// <param name="id">Identifier of this node in the "values" map (see <see cref="TryMatch"/>)</param>
    public static CodeTreePattern BinaryOperationNode(
        Predicate<BinaryOperation> operation,
        out CodeTreePattern id,
        CodeTreePattern left,
        CodeTreePattern right) => id = new BinaryOperationNodePattern(operation, left, right);

    /// <summary>
    /// <see cref="UnaryOperationNode"/> pattern,
    /// allowing to filter unary-operator of the matching node via <see cref="operation"/> predicate.
    /// </summary>
    /// <param name="id">Identifier of this node in the "values" map (see <see cref="TryMatch"/>)</param>
    public static CodeTreePattern UnaryOperationNode(
        Predicate<UnaryOperation> operation,
        out CodeTreePattern id,
        CodeTreePattern operand) => id = new UnaryOperationNodePattern(operation, operand);

    /// <summary>
    /// <see cref="Constant"/> pattern,
    /// allowing to filter constant value of the matching node via <see cref="value"/> predicate.
    /// </summary>
    /// <param name="id">Identifier of this node in the "values" map (see <see cref="TryMatch"/>)</param>
    public static CodeTreePattern Constant(
        Predicate<RegisterValue> value,
        out CodeTreePattern id) => id = new ConstantPattern(value);

    /// <summary>
    /// <see cref="RegisterRead"/> pattern,
    /// allowing to filter register of the matching node via <see cref="Register"/> predicate.
    /// </summary>
    /// <param name="id">Identifier of this node in the "values" map (see <see cref="TryMatch"/>)</param>
    public static CodeTreePattern RegisterRead(
        Predicate<Register> register,
        out CodeTreePattern id) => id = new RegisterReadPattern(register);

    /// <summary>
    /// <see cref="RegisterWrite"/> pattern,
    /// allowing to filter register of the matching node via <see cref="Register"/> predicate.
    /// </summary>
    /// <param name="id">Identifier of this node in the "values" map (see <see cref="TryMatch"/>)</param>
    public static CodeTreePattern RegisterWrite(
        Predicate<Register> register,
        out CodeTreePattern id,
        CodeTreePattern value) => id = new RegisterWritePattern(register, value);

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
    /// <see cref="FunctionCall"/> pattern.
    /// </summary>
    /// <param name="id">Identifier of this node in the "values" map (see <see cref="TryMatch"/>)</param>
    public static CodeTreePattern FunctionCall(out CodeTreePattern id) => id = new FunctionCallPattern();

    /// <summary>
    /// Wildcard pattern, which matches any <see cref="CodeTreeValueNode"/>.
    /// </summary>
    public static CodeTreePattern WildcardNode => new WildcardNodePattern();

    private sealed record BinaryOperationNodePattern(
        Predicate<BinaryOperation> Operation,
        CodeTreePattern Left,
        CodeTreePattern Right) : CodeTreePattern
    {
        public override bool TryMatch(CodeTreeNode root,
            [NotNullWhen(true)] out IEnumerable<CodeTreeValueNode>? leaves,
            IDictionary<CodeTreePattern, object> values)
        {
            leaves = null;
            return root is BinaryOperationNode node &&
                   Operation.Invoke(node.Operation) &&
                   Run(values[this] = node.Operation) &&
                   Left.TryMatch(node.Left, out var leftLeaves, values) &&
                   Right.TryMatch(node.Right, out var rightLeaves, values) &&
                   Run(leaves = leftLeaves.Concat(rightLeaves));
        }
    }

    private sealed record UnaryOperationNodePattern(Predicate<UnaryOperation> Operation, CodeTreePattern Operand) : CodeTreePattern
    {
        public override bool TryMatch(CodeTreeNode root,
            [NotNullWhen(true)] out IEnumerable<CodeTreeValueNode>? leaves,
            IDictionary<CodeTreePattern, object> values)
        {
            leaves = null;
            return root is UnaryOperationNode node &&
                   Operation.Invoke(node.Operation) &&
                   Run(values[this] = node.Operation) &&
                   Operand.TryMatch(node.Operand, out leaves, values);
        }
    }

    private sealed record ConstantPattern(Predicate<RegisterValue> Value) : CodeTreePattern
    {
        public override bool TryMatch(CodeTreeNode root,
            [NotNullWhen(true)] out IEnumerable<CodeTreeValueNode>? leaves,
            IDictionary<CodeTreePattern, object> values)
        {
            leaves = Enumerable.Empty<CodeTreeValueNode>();
            return root is Constant node &&
                   Value.Invoke(node.Value) &&
                   Run(values[this] = node.Value);
        }
    }

    private sealed record RegisterReadPattern(Predicate<Register> Register) : CodeTreePattern
    {
        public override bool TryMatch(CodeTreeNode root,
            [NotNullWhen(true)] out IEnumerable<CodeTreeValueNode>? leaves,
            IDictionary<CodeTreePattern, object> values)
        {
            leaves = Enumerable.Empty<CodeTreeValueNode>();
            return root is RegisterRead node &&
                   Register.Invoke(node.Register) &&
                   Run(values[this] = node.Register);
        }
    }

    private sealed record RegisterWritePattern(Predicate<Register> Register, CodeTreePattern Value) : CodeTreePattern
    {
        public override bool TryMatch(CodeTreeNode root,
            [NotNullWhen(true)] out IEnumerable<CodeTreeValueNode>? leaves,
            IDictionary<CodeTreePattern, object> values)
        {
            leaves = null;
            return root is RegisterWrite node &&
                   Register.Invoke(node.Register) &&
                   Run(values[this] = node.Register) &&
                   Value.TryMatch(node.Value, out leaves, values);
        }
    }

    private sealed record MemoryReadPattern(CodeTreePattern MemoryLocation) : CodeTreePattern
    {
        public override bool TryMatch(CodeTreeNode root,
            [NotNullWhen(true)] out IEnumerable<CodeTreeValueNode>? leaves,
            IDictionary<CodeTreePattern, object> values)
        {
            leaves = null;
            return root is MemoryRead node &&
                   MemoryLocation.TryMatch(node.MemoryLocation, out leaves, values);
        }
    }

    private sealed record MemoryWritePattern(CodeTreePattern MemoryLocation, CodeTreePattern Value) : CodeTreePattern
    {
        public override bool TryMatch(CodeTreeNode root,
            [NotNullWhen(true)] out IEnumerable<CodeTreeValueNode>? leaves,
            IDictionary<CodeTreePattern, object> values)
        {
            leaves = null;
            return root is MemoryWrite node &&
                   MemoryLocation.TryMatch(node.MemoryLocation, out var leavesLocation, values) &&
                   Value.TryMatch(node.Value, out var leavesValue, values) &&
                   Run(leaves = leavesLocation.Concat(leavesValue));
        }
    }

    private sealed record FunctionCallPattern : CodeTreePattern
    {
        public override bool TryMatch(CodeTreeNode root,
            [NotNullWhen(true)] out IEnumerable<CodeTreeValueNode>? leaves,
            IDictionary<CodeTreePattern, object> values)
        {
            leaves = Enumerable.Empty<CodeTreeValueNode>();
            return root is FunctionCall node &&
                   Run(values[this] = node.FunctionCaller);
        }
    }

    private sealed record WildcardNodePattern : CodeTreePattern
    {
        public override bool TryMatch(CodeTreeNode root,
            [NotNullWhen(true)] out IEnumerable<CodeTreeValueNode>? leaves,
            IDictionary<CodeTreePattern, object> _)
        {
            if (root is not CodeTreeValueNode rootValue)
            {
                leaves = null;
                return false;
            }

            leaves = Enumerable.Repeat(rootValue, 1);
            return true;
        }
    }

    // hack method, which allows to treat assignments as "true" values
    private static bool Run<T>(T assignment) => assignment is not null;
}
