namespace sernick.Compiler.Function;

using ControlFlowGraph.CodeTree;
using static ControlFlowGraph.CodeTree.CodeTreeExtensions;

public sealed class FunctionContext : IFunctionContext
{
    private static readonly HardwareRegister[] calleeToSave = {
        HardwareRegister.R12,
        HardwareRegister.R13,
        HardwareRegister.R14,
        HardwareRegister.R15,
        HardwareRegister.RBX,
        HardwareRegister.RDI,
        HardwareRegister.RSI,
    };

    private static readonly HardwareRegister[] callerToSave = {
        HardwareRegister.R8,
        HardwareRegister.R9,
        HardwareRegister.R10,
        HardwareRegister.R11,
        HardwareRegister.RAX,
        HardwareRegister.RCX,
        HardwareRegister.RDX,
    };

    private static readonly HardwareRegister[] argumentRegisters = {
        HardwareRegister.RDI,
        HardwareRegister.RSI,
        HardwareRegister.RDX,
        HardwareRegister.RCX,
        HardwareRegister.R8,
        HardwareRegister.R9,
    };

    private const int PointerSize = 8;
    private readonly IFunctionContext? _parentContext;
    private readonly IReadOnlyList<IFunctionParam> _functionParameters;
    private readonly bool _valueIsReturned;

    // Maps accesses to registers/memory
    private readonly Dictionary<IFunctionVariable, VariableLocation> _localVariableLocation;
    private int _localsOffset;
    private readonly CodeTreeValueNode _displayEntry;
    private readonly Register _oldDisplayValReg;
    private readonly Dictionary<HardwareRegister, Register> _registerToTemporaryMap;
    public int Depth { get; }
    public FunctionContext(
        IFunctionContext? parent,
        IReadOnlyList<IFunctionParam> parameters,
        bool returnsValue
        )
    {
        _localVariableLocation = new Dictionary<IFunctionVariable, VariableLocation>(ReferenceEqualityComparer.Instance);
        _parentContext = parent;
        _functionParameters = parameters;
        _valueIsReturned = returnsValue;
        _localsOffset = 0;
        _displayEntry = new GlobalAddress("display") + PointerSize * Depth;
        _registerToTemporaryMap = calleeToSave.ToDictionary<HardwareRegister, HardwareRegister, Register>(reg => reg, _ => new Register(), ReferenceEqualityComparer.Instance);
        _oldDisplayValReg = new Register();

        Depth = (parent?.Depth + 1) ?? 0;

        var fistArgOffset = PointerSize * (1 + _functionParameters.Count - 6);
        var argNum = 0;
        for (var i = 6; i < _functionParameters.Count; i++)
        {
            _localVariableLocation.Add(_functionParameters[i], new MemoryLocation(-(fistArgOffset - PointerSize * argNum)));
            argNum += 1;
        }
    }
    public void AddLocal(IFunctionVariable variable, bool usedElsewhere)
    {
        if (usedElsewhere)
        {
            if (_localVariableLocation.TryAdd(variable, new MemoryLocation(_localsOffset + PointerSize)))
            {
                _localsOffset += PointerSize;
            }
        }
        else
        {
            _localVariableLocation.TryAdd(variable, new RegisterLocation());
        }
    }

    public IFunctionCaller.GenerateCallResult GenerateCall(IReadOnlyList<CodeTreeValueNode> arguments)
    {
        var operations = new List<CodeTreeNode>();

        // Caller-saved registers
        var callerSavedMap = new Dictionary<HardwareRegister, Register>(ReferenceEqualityComparer.Instance);
        foreach (var reg in callerToSave)
        {
            var tempReg = new Register();
            callerSavedMap[reg] = tempReg;
            operations.Add(Reg(tempReg).Write(Reg(reg).Read()));
        }

        Register rsp = HardwareRegister.RSP;
        Register rax = HardwareRegister.RAX;

        var rspRead = Reg(rsp).Read();
        var pushRsp = Reg(rsp).Write(rspRead - PointerSize);

        // Divide args into register and stack
        var regArgs = new List<CodeTreeValueNode>();
        var stackArgs = new List<CodeTreeValueNode>();

        for (var i = 0; i < arguments.Count; i++)
        {
            (i < 6 ? regArgs : stackArgs).Add(arguments[i]);
        }

        // Put args into registers
        operations.AddRange(argumentRegisters.Zip(regArgs).Select(p => Reg(p.First).Write(p.Second)));

        // Put args onto stack
        foreach (var arg in stackArgs)
        {
            operations.Add(pushRsp);
            operations.Add(Mem(rspRead).Write(arg));
        }

        // Performing actual call (puts return addess on stack and jumps)
        operations.Add(new FunctionCall(this));

        // Remove arguments from stack (we already returned from call)
        operations.Add(Reg(rsp).Write(rspRead + PointerSize * arguments.Count));

        // If value is returned, then put it from RAX to virtual register
        CodeTreeValueNode? returnValueLocation = null;
        if (_valueIsReturned)
        {
            var returnValueRegister = new Register();
            var raxRead = Reg(rax).Read();
            operations.Add(Reg(returnValueRegister).Write(raxRead));
            returnValueLocation = Reg(returnValueRegister).Read();
        }

        // Retrieve values of caller-saved registers
        foreach (var reg in calleeToSave)
        {
            var tempReg = callerSavedMap[reg];
            var tempVal = Reg(tempReg).Read();
            operations.Add(Reg(reg).Write(tempVal));
        }

        return new IFunctionCaller.GenerateCallResult(CodeTreeListToSingleExitList(operations), returnValueLocation);
    }

    public IReadOnlyList<SingleExitNode> GeneratePrologue()
    {
        var operations = new List<CodeTreeNode>();

        var rsp = HardwareRegister.RSP;
        var rbp = HardwareRegister.RBP;

        var rspRead = Reg(rsp).Read();
        var pushRsp = Reg(rsp).Write(rspRead - PointerSize);
        var rbpRead = Reg(rbp).Read();

        // Allocate slot for old RBP value
        operations.Add(pushRsp);

        // Write down old RBP value
        operations.Add(Mem(rspRead).Write(rbpRead));

        // Set new RBP value
        operations.Add(Reg(rbp).Write(rspRead));

        // Save and update display entry
        operations.Add(Reg(_oldDisplayValReg).Write(Mem(_displayEntry).Read()));
        operations.Add(Mem(_displayEntry).Write(rbpRead));

        // Allocate memory for variables
        operations.Add(Reg(rsp).Write(rspRead - _localsOffset));

        // Write arguments to known locations
        var paramNum = _functionParameters.Count;
        var regParamsNum = (paramNum > 6 ? 6 : paramNum);
        for (var i = 0; i < regParamsNum; i++)
        {
            GenerateVariableWrite(_functionParameters[i], Reg(argumentRegisters[i]).Read());
        }

        // Callee-saved registers
        foreach (var reg in calleeToSave)
        {
            var tempReg = _registerToTemporaryMap[reg];
            var regVal = Reg(reg).Read();
            operations.Add(Reg(tempReg).Write(regVal));
        }

        return CodeTreeListToSingleExitList(operations);
    }

    public IReadOnlyList<SingleExitNode> GenerateEpilogue(CodeTreeValueNode? valToReturn)
    {
        var operations = calleeToSave.Select(reg =>
            Reg(reg).Write(Reg(_registerToTemporaryMap[reg]).Read())).ToList<CodeTreeNode>();

        var rsp = HardwareRegister.RSP;
        var rbp = HardwareRegister.RBP;
        var rax = HardwareRegister.RAX;

        var rspRead = Reg(rsp).Read();

        // Free local variables stack space
        operations.Add(Reg(rsp).Write(rspRead + _localsOffset));

        // Retrieve old RBP
        operations.Add(Reg(rbp).Write(Mem(rspRead).Read()));

        // Free RBP slot
        operations.Add(Reg(rsp).Write(rspRead + PointerSize));

        // Restore old display value
        operations.Add(Mem(_displayEntry).Write(Reg(_oldDisplayValReg).Read()));

        // Save return value to rax
        if (valToReturn != null)
        {
            operations.Add(Reg(rax).Write(valToReturn));
        }

        return CodeTreeListToSingleExitList(operations);
    }

    private static List<SingleExitNode> CodeTreeListToSingleExitList(List<CodeTreeNode> trees)
    {
        var result = new List<SingleExitNode>();
        trees.Reverse();
        SingleExitNode? nextRoot = null;
        foreach (var tree in trees)
        {
            var treeRoot = new SingleExitNode(nextRoot, new List<CodeTreeNode> { tree });
            result.Add(treeRoot);
            nextRoot = treeRoot;
        }

        result.Reverse();
        return result;
    }

    public CodeTreeValueNode GenerateVariableRead(IFunctionVariable variable) =>
        _localVariableLocation.TryGetValue(variable, out var location)
            ? location.GenerateRead()
            : new MemoryRead(GetParentsIndirectVariableLocation(variable));

    public CodeTreeNode GenerateVariableWrite(IFunctionVariable variable, CodeTreeValueNode value) =>
        _localVariableLocation.TryGetValue(variable, out var location)
            ? location.GenerateWrite(value)
            : new MemoryWrite(GetParentsIndirectVariableLocation(variable), value);

    CodeTreeValueNode IFunctionContext.GetIndirectVariableLocation(IFunctionVariable variable)
    {
        if (!_localVariableLocation.TryGetValue(variable, out var local))
        {
            // If variable isn't in this context then it should be is the context of some ancestor.
            return _parentContext?.GetIndirectVariableLocation(variable) ??
                   throw new ArgumentException("Variable is undefined");
        }

        if (local is not MemoryLocation localMemory)
        {
            throw new ArgumentException(
                "Variable was added with usedElsewhere=false and can't be accessed indirectly",
                nameof(variable));
        }

        return Mem(_displayEntry).Read() - localMemory.Offset;
    }

    private CodeTreeValueNode GetParentsIndirectVariableLocation(IFunctionVariable variable)
    {
        if (_parentContext == null)
        {
            // Get indirect location from ancestors' contexts or throw an error if variable wasn't defined in any context.
            throw new ArgumentException("Variable is undefined");
        }

        return _parentContext.GetIndirectVariableLocation(variable);
    }
}

internal abstract record VariableLocation
{
    public abstract CodeTreeValueNode GenerateRead();
    public abstract CodeTreeNode GenerateWrite(CodeTreeValueNode value);
}

internal record MemoryLocation(CodeTreeValueNode Offset) : VariableLocation
{
    private readonly CodeTreeValueNode _directLocation = Reg(HardwareRegister.RBP).Read() - Offset;
    public override CodeTreeValueNode GenerateRead() => new MemoryRead(_directLocation);

    public override CodeTreeNode GenerateWrite(CodeTreeValueNode value) => new MemoryWrite(_directLocation, value);
}

internal record RegisterLocation : VariableLocation
{
    private readonly Register _register = new();
    public override CodeTreeValueNode GenerateRead() =>
        new RegisterRead(_register);

    public override CodeTreeNode GenerateWrite(CodeTreeValueNode value) =>
        new RegisterWrite(_register, value);
}
