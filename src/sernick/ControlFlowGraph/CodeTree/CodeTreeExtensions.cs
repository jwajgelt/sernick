namespace sernick.ControlFlowGraph.CodeTree;

public abstract partial record CodeTreeNode
{
    public static BinaryOperationNode operator +(CodeTreeNode left, CodeTreeNode right) =>
        new(BinaryOperation.Add, left, right);

    public static BinaryOperationNode operator -(CodeTreeNode left, CodeTreeNode right) =>
        new(BinaryOperation.Sub, left, right);

    public static BinaryOperationNode operator *(CodeTreeNode left, CodeTreeNode right) =>
        new(BinaryOperation.Mul, left, right);

    public static BinaryOperationNode operator &(CodeTreeNode left, CodeTreeNode right) =>
        new(BinaryOperation.BitwiseAnd, left, right);

    public static BinaryOperationNode operator |(CodeTreeNode left, CodeTreeNode right) =>
        new(BinaryOperation.BitwiseOr, left, right);

    public static BinaryOperationNode operator <(CodeTreeNode left, CodeTreeNode right) =>
        new(BinaryOperation.LessThan, left, right);

    public static BinaryOperationNode operator >(CodeTreeNode left, CodeTreeNode right) =>
        new(BinaryOperation.GreaterThan, left, right);

    public static BinaryOperationNode operator <=(CodeTreeNode left, CodeTreeNode right) =>
        new(BinaryOperation.LessThanEqual, left, right);

    public static BinaryOperationNode operator >=(CodeTreeNode left, CodeTreeNode right) =>
        new(BinaryOperation.GreaterThanEqual, left, right);

    public static UnaryOperationNode operator !(CodeTreeNode operand) =>
        new(UnaryOperation.Not, operand);

    public static UnaryOperationNode operator ~(CodeTreeNode operand) =>
        new(UnaryOperation.Negate, operand);

    public static implicit operator CodeTreeNode(long constant) => new Constant(new RegisterValue(constant));
}

/// <summary>
/// Simple factory extensions for Memory and Register CodeTree nodes.
/// <example>
/// using static CodeTreeExtensions;
///
/// Reg(myRegister).Write(Mem(loc).Value);
/// </example>
/// </summary>
public static class CodeTreeExtensions
{
    public static MemoryReference Mem(CodeTreeNode location) => new(location);

    /// <summary>
    /// Create using <see cref="CodeTreeExtensions.Mem"/> method.
    /// Then you can call <see cref="Write"/>, <see cref="Read"/> or <see cref="Value"/>,
    /// which returns a corresponding <see cref="CodeTreeNode"/>.
    /// </summary>
    public sealed class MemoryReference
    {
        private readonly CodeTreeNode _location;
        internal MemoryReference(CodeTreeNode location) => _location = location;

        public MemoryWrite Write(CodeTreeNode value) => new(_location, value);

        public MemoryRead Value => new(_location);
        public MemoryRead Read() => Value;
    }

    public static RegisterReference Reg(Register register) => new(register);

    /// <summary>
    /// Create using <see cref="CodeTreeExtensions.Reg"/> method.
    /// Then you can call <see cref="Write"/>, <see cref="Read"/> or <see cref="Value"/>,
    /// which returns a corresponding <see cref="CodeTreeNode"/>.
    /// </summary>
    public sealed class RegisterReference
    {
        private readonly Register _register;
        internal RegisterReference(Register register) => _register = register;

        public RegisterWrite Write(CodeTreeNode value) => new(_register, value);

        public RegisterRead Value => new(_register);
        public RegisterRead Read() => Value;
    }
}
