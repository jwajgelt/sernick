namespace sernick.ControlFlowGraph.CodeTree;

using Compiler.Function;

/// <summary>
/// Struct for values which can fit in a register
/// </summary>
public record struct RegisterValue(long Value)
{
    public override string ToString() => $"{Value}";
}

/// <summary>
/// Base class nodes appear in CodeTree, which corresponds to a single node in our control-flow graph
/// </summary>
public abstract record CodeTreeNode;

/// <summary>
/// Class of nodes which calculate a value
/// </summary>
public abstract partial record CodeTreeValueNode : CodeTreeNode;

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
    (BinaryOperation Operation, CodeTreeValueNode Left, CodeTreeValueNode Right) : CodeTreeValueNode
{
    public override string ToString() => $"{Left} {Operation} {Right}";
}

public enum UnaryOperation
{
    Not, Negate
}

public sealed record UnaryOperationNode(UnaryOperation Operation, CodeTreeValueNode Operand) : CodeTreeValueNode
{
    public override string ToString() => $"{Operation} {Operand}";
}

/// <summary>
/// Use new Register() if you want to say "I want to save the value, preferably to a register, but
/// in the worst-case we could save it to memory". Then you'll get *some* register or memory location.
/// Use static variables of HardwareRegister class if you want to get the exact register (one of callee-saved)
/// </summary>
public class Register
{
    public override string ToString() => $"Reg{GetHashCode() % 100}";
}

#pragma warning disable IDE0052
/// <summary>
/// https://en.wikipedia.org/wiki/X86_calling_conventions#Callee-saved_(non-volatile)_registers
/// List of callee-preserved registers is below
/// https://stackoverflow.com/questions/18024672/what-registers-are-preserved-through-a-linux-x86-64-function-call
/// </summary>
public class HardwareRegister : Register
{
    private HardwareRegister() { }
    public static readonly HardwareRegister RAX = new();
    public static readonly HardwareRegister RBX = new();
    public static readonly HardwareRegister RCX = new();
    public static readonly HardwareRegister RDX = new();

    public static readonly HardwareRegister RSP = new();
    public static readonly HardwareRegister RBP = new();

    public static readonly HardwareRegister RDI = new();
    public static readonly HardwareRegister RSI = new();

    public static readonly HardwareRegister R8 = new();
    public static readonly HardwareRegister R9 = new();
    public static readonly HardwareRegister R10 = new();
    public static readonly HardwareRegister R11 = new();
    public static readonly HardwareRegister R12 = new();
    public static readonly HardwareRegister R13 = new();
    public static readonly HardwareRegister R14 = new();
    public static readonly HardwareRegister R15 = new();
}

public sealed record RegisterRead(Register Register) : CodeTreeValueNode
{
    public override string ToString() => $"{Register}";
}
public sealed record RegisterWrite(Register Register, CodeTreeValueNode Value) : CodeTreeNode
{
    public override string ToString() => $"{Register} = {Value}";
}

public sealed record GlobalAddress(string Label) : CodeTreeValueNode
{
    public override string ToString() => $"{Label}";
}

public sealed record MemoryRead(CodeTreeValueNode MemoryLocation) : CodeTreeValueNode
{
    public override string ToString() => $"Mem({MemoryLocation})";
}
public sealed record MemoryWrite(CodeTreeValueNode MemoryLocation, CodeTreeValueNode Value) : CodeTreeNode
{
    public override string ToString() => $"Mem({MemoryLocation}) = {Value}";
}

public sealed record Constant(RegisterValue Value) : CodeTreeValueNode
{
    public override string ToString() => $"{Value}";
}

public sealed record FunctionCall(IFunctionCaller FunctionCaller) : CodeTreeNode
{
    public override string ToString() => $"{FunctionCaller.Label}";
}

public sealed record FunctionReturn() : CodeTreeNode
{
    public override string ToString() => $"Ret";
}
