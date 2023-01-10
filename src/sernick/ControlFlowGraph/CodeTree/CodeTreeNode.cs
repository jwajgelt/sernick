namespace sernick.ControlFlowGraph.CodeTree;

using CodeGeneration;
using Compiler.Function;

/// <summary>
/// Struct for values which can fit in a register
/// </summary>
public sealed record RegisterValue
{
    public RegisterValue(long value, bool isFinal = true)
    {
        Value = value;
        IsFinal = isFinal;
    }

    public long Value { get; set; }
    public bool IsFinal { get; }
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
    private readonly string _label;
    protected HardwareRegister(string label, string quadWord, string doubleWord, string word, string b)
    {
        _label = label;
        QuadWord = quadWord;
        DoubleWord = doubleWord;
        Word = word;
        Byte = b;
    }
    public override string ToString() => _label;

    public string QuadWord { get; }
    public string DoubleWord { get; }
    public string Word { get; }
    public string Byte { get; }

    public bool Equals(HardwareRegister? other) => _label.Equals(other?._label);
    public override bool Equals(object? obj) => obj is HardwareRegister other && Equals(other);
    public override int GetHashCode() => _label.GetHashCode();

    public static readonly HardwareRegister RAX = new("RAX", "rax", "eax", "ax", "al");
    public static readonly HardwareRegister RBX = new("RBX", "rbx", "ebx", "bx", "bl");
    public static readonly HardwareRegister RCX = new("RCX", "rcx", "ecx", "cx", "cl");
    public static readonly HardwareRegister RDX = new("RDX", "rdx", "edx", "dx", "dl");

    public static readonly HardwareRegister RSP = new("RSP", "rsp", "esp", "sp", "spl");
    public static readonly HardwareRegister RBP = new("RBP", "rbp", "ebp", "bp", "bpl");

    public static readonly HardwareRegister RDI = new("RDI", "rdi", "edi", "di", "dil");
    public static readonly HardwareRegister RSI = new("RSI", "rsi", "esi", "si", "sil");

    public static readonly HardwareRegister R8 = new("R8", "r8", "r8d", "r8w", "r8b");
    public static readonly HardwareRegister R9 = new("R9", "r9", "r9d", "r9w", "r9b");
    public static readonly HardwareRegister R10 = new("R10", "r10", "r10d", "r10w", "r10b");
    public static readonly HardwareRegister R11 = new("R11", "r11", "r11d", "r11w", "r11b");
    public static readonly HardwareRegister R12 = new("R12", "r12", "r12d", "r12w", "r12b");
    public static readonly HardwareRegister R13 = new("R13", "r13", "r13d", "r13w", "r13b");
    public static readonly HardwareRegister R14 = new("R14", "r14", "r14d", "r14w", "r14b");
    public static readonly HardwareRegister R15 = new("R15", "r15", "r15d", "r15w", "r15b");
}

public sealed record RegisterRead(Register Register) : CodeTreeValueNode
{
    public override string ToString() => $"{Register}";
}
public sealed record RegisterWrite(Register Register, CodeTreeValueNode Value) : CodeTreeNode
{
    public override string ToString() => $"{Register} = {Value}";
}

public sealed record GlobalAddress(Label Label) : CodeTreeValueNode
{
    public override string ToString() => $"{Label.Value}";
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
    public override string ToString() => $"Call {FunctionCaller.Label.Value}";

    public override int GetHashCode() => FunctionCaller.Label.GetHashCode();
    public bool Equals(FunctionCall? other) => FunctionCaller.Label.Equals(other?.FunctionCaller.Label);
}

public sealed record FunctionReturn : CodeTreeNode
{
    public override string ToString() => "Ret";
}
