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
    BitwiseAnd, BitwiseOr,
}

public sealed record BinaryOperationNode
    (BinaryOperation Operation, CodeTreeNode Left, CodeTreeNode Right) : CodeTreeNode;

public enum UnaryOperation
{
    Not, Negate
}

public sealed record UnaryOperationNode(UnaryOperation Operation, CodeTreeNode Operand) : CodeTreeNode;

/// <summary>
/// Use new Register() if you want to say "I want to save the value, preferably to a register, but
/// in the worst-case we could save it to memory". Then you'll get *some* register or memory location.
/// Use static variables of HardwareRegister class if you want to get the exact register (one of callee-saved)
/// </summary>
public class Register { }

/// <summary>
/// Do not use a constructor -- use one of 16 static variables of this class
/// https://en.wikipedia.org/wiki/X86_calling_conventions#Callee-saved_(non-volatile)_registers
/// </summary>
public class HardwareRegister: Register {
    static HardwareRegister RBP = new HardwareRegister();
    static HardwareRegister RBX = new HardwareRegister();
    static HardwareRegister R4 = new HardwareRegister();
    static HardwareRegister R5 = new HardwareRegister();
    static HardwareRegister R6 = new HardwareRegister();
    static HardwareRegister R7 = new HardwareRegister();
    static HardwareRegister R8 = new HardwareRegister();
    static HardwareRegister R9 = new HardwareRegister();
    static HardwareRegister R10 = new HardwareRegister();
    // TODO more registers
}

public sealed record RegisterRead(Register Register) : CodeTreeNode;

public sealed record RegisterWrite(Register Register, CodeTreeNode Value) : CodeTreeNode;

public sealed record MemoryRead(CodeTreeNode MemoryLocation) : CodeTreeNode;
public sealed record MemoryWrite(CodeTreeNode MemoryLocation, CodeTreeNode Value) : CodeTreeNode;

public sealed record Constant(RegisterValue Value) : CodeTreeNode;

public sealed record FunctionCall(IFunctionCaller FunctionCaller, IEnumerable<CodeTreeNode> Arguments) : CodeTreeNode;
