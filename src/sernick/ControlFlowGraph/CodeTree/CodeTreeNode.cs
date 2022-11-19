namespace sernick.ControlFlowGraph.CodeTree;

using Compiler.Function;

/// <summary>
/// Struct for values which can fit in a register
/// </summary>
public record struct RegisterValue(long Value);

/// <summary>
/// Base class nodes appear in CodeTree, which corresponds to a single node in our control-flow graph
/// </summary>
public abstract record CodeTreeNode;

/// <summary>
/// All binary operations used in the code trees
/// </summary>
public enum BinaryOperation
{
    Add, Sub, Mul,
    LessThan, GreaterThan,
    LessThanEqual, GreaterThanEqual,
    Equal, NotEqual,
    Not, And, Or,
}

public sealed record BinaryOperationNode
    (BinaryOperation Operation, CodeTreeNode Left, CodeTreeNode Right) : CodeTreeNode;

public sealed record Register;

public sealed record RegisterRead(Register Register) : CodeTreeNode;

public sealed record RegisterWrite(Register Register, CodeTreeNode Value) : CodeTreeNode;

public sealed record MemoryRead(CodeTreeNode MemoryLocation) : CodeTreeNode;
public sealed record MemoryWrite(CodeTreeNode MemoryLocation, CodeTreeNode Value) : CodeTreeNode;

public sealed record Constant(RegisterValue Value) : CodeTreeNode;

public sealed record FunctionCall(IFunctionCaller FunctionCaller, IEnumerable<CodeTreeNode> Arguments) : CodeTreeNode;
