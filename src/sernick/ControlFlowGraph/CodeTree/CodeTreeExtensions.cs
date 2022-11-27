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
/// Reg(myRegister, Mem(loc));
/// </example>
/// </summary>
public static class CodeTreeExtensions
{
    public static MemoryRead Mem(CodeTreeNode location) => new(location);
    public static MemoryWrite Mem(CodeTreeNode location, CodeTreeNode value) => new(location, value);

    public static RegisterRead Reg(Register register) => new(register);
    public static RegisterWrite Reg(Register register, CodeTreeNode value) => new(register, value);
}
