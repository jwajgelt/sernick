namespace sernick.Compiler.Function;

using CodeGeneration;
using ControlFlowGraph.CodeTree;
using static ControlFlowGraph.CodeTree.CodeTreeExtensions;
using static ControlFlowGraph.CodeTree.StructHelper;
using static Convention;
using static Helpers;
using static PlatformConstants;
using FunctionCall = ControlFlowGraph.CodeTree.FunctionCall;

public sealed class FunctionContext : IFunctionContext
{
    private readonly IFunctionContext? _parentContext;
    private readonly IReadOnlyList<IFunctionParam> _functionParameters;

    // Maps accesses to registers/memory
    private readonly Dictionary<IFunctionVariable, VariableLocation> _localVariableLocation;
    private readonly Dictionary<IFunctionVariable, int> _localVariableSize;
    private readonly Dictionary<IFunctionVariable, bool> _localVariableIsStruct;
    private readonly RegisterValue _localsOffset;
    private readonly CodeTreeValueNode _displayEntry;
    private readonly Register _oldDisplayValReg;
    private readonly int? _returnedStructSize;
    private readonly Dictionary<HardwareRegister, Register> _registerToTemporaryMap;

    public IFunctionContext? ParentContext { get; }
    public Label Label { get; }
    public int Depth { get; }
    public bool ValueIsReturned { get; }
    public IReadOnlyDictionary<IFunctionVariable, int> LocalVariableSize => _localVariableSize;
    public IReadOnlyDictionary<IFunctionVariable, bool> LocalVariableIsStruct => _localVariableIsStruct;

    public FunctionContext(
        IFunctionContext? parent,
        IReadOnlyList<IFunctionParam> parameters,
        bool returnsValue,
        Label label,
        int? returnedStructSize = null
    )
    {
        Label = label;
        ParentContext = parent;
        Depth = parent?.Depth + 1 ?? 0;
        ValueIsReturned = returnsValue;

        _localVariableLocation = new Dictionary<IFunctionVariable, VariableLocation>(ReferenceEqualityComparer.Instance);
        _localVariableSize = new Dictionary<IFunctionVariable, int>(ReferenceEqualityComparer.Instance);
        _localVariableIsStruct = new Dictionary<IFunctionVariable, bool>(ReferenceEqualityComparer.Instance);
        _parentContext = parent;
        _functionParameters = parameters;
        _registerToTemporaryMap = CalleeToSave.ToDictionary<HardwareRegister, HardwareRegister, Register>(reg => reg, _ => new Register(), ReferenceEqualityComparer.Instance);
        _localsOffset = new RegisterValue(0, false);
        _displayEntry = new GlobalAddress(DisplayTable.DISPLAY_TABLE_SYMBOL) + POINTER_SIZE * Depth;
        _oldDisplayValReg = new Register();
        _returnedStructSize = returnedStructSize;

        var fistArgOffset = POINTER_SIZE * (2 + _functionParameters.Count - REG_ARGS_COUNT);
        var argNum = 0;
        for (var i = REG_ARGS_COUNT; i < _functionParameters.Count; i++)
        {
            _localVariableLocation.Add(_functionParameters[i], new MemoryLocation(-(fistArgOffset - POINTER_SIZE * argNum)));
            _localVariableSize.Add(_functionParameters[i], POINTER_SIZE);
            _localVariableIsStruct.Add(_functionParameters[i], false);
            argNum += 1;
        }
    }

    public void AddLocal(IFunctionVariable variable, bool usedElsewhere = false, bool isStruct = false, int size = POINTER_SIZE)
    {
        if (isStruct || usedElsewhere)
        {
            if (_localVariableLocation.TryAdd(variable, new MemoryLocation(_localsOffset.Value + size)))
            {
                _localsOffset.Value += size;
            }
        }
        else
        {
            _localVariableLocation.TryAdd(variable, new RegisterLocation());
        }

        _localVariableSize.TryAdd(variable, size);
        _localVariableIsStruct.TryAdd(variable, isStruct);
    }

    public IFunctionCaller.GenerateCallResult GenerateCall(IReadOnlyList<CodeTreeValueNode> arguments)
    {
        var operations = new List<CodeTreeNode>();

        // Caller-saved registers
        var callerSavedMap = new Dictionary<HardwareRegister, Register>(ReferenceEqualityComparer.Instance);
        foreach (var reg in CallerToSave)
        {
            var tempReg = new Register();
            callerSavedMap[reg] = tempReg;
            operations.Add(Reg(tempReg).Write(Reg(reg).Read()));
        }

        Register rsp = HardwareRegister.RSP;
        Register rax = HardwareRegister.RAX;

        var rspRead = Reg(rsp).Read();
        var pushRsp = Reg(rsp).Write(rspRead - POINTER_SIZE);

        // Add default arguments if necessary
        var allArgs = new List<CodeTreeValueNode>(arguments);
        while (allArgs.Count < _functionParameters.Count)
        {
            allArgs.Add(_functionParameters[allArgs.Count].GetDefaultValue());
        }

        // Divide args into register and stack
        var regArgs = new List<CodeTreeValueNode>();
        var stackArgs = new List<CodeTreeValueNode>();

        for (var i = 0; i < allArgs.Count; i++)
        {
            (i < REG_ARGS_COUNT ? regArgs : stackArgs).Add(allArgs[i]);
        }

        // Put args into registers
        operations.AddRange(ArgumentRegisters.Zip(regArgs).Select(p => Reg(p.First).Write(p.Second)));

        // Put args onto stack
        foreach (var arg in stackArgs)
        {
            operations.Add(pushRsp);
            operations.Add(Mem(rspRead).Write(arg));
        }

        // Align the stack
        var tmpRsp = Reg(new Register());
        operations.Add(tmpRsp.Write(rspRead));
        operations.Add(Reg(rsp).Write(rspRead & -2 * POINTER_SIZE));

        // Performing actual call (puts return address on stack and jumps)
        operations.Add(new FunctionCall(this));

        // Restore stack pointer
        operations.Add(Reg(rsp).Write(tmpRsp.Read()));

        // Remove arguments from stack (we already returned from call)
        operations.Add(Reg(rsp).Write(rspRead + POINTER_SIZE * stackArgs.Count));

        if (!ValueIsReturned)
        {
            return new IFunctionCaller.GenerateCallResult(CodeTreeListToSingleExitList(operations),
                null);
        }

        // If value is returned, then put it from RAX to virtual register
        CodeTreeValueNode? returnValueLocation = null;
        if (_returnedStructSize is not null)
        {
            var raxRead = Reg(rax).Read();
            operations.AddRange(GenerateStructCopy(GenerateVariableRead(_functionParameters[0]), raxRead, _returnedStructSize.GetValueOrDefault()));
        }

        else if (ValueIsReturned)
        {
            var returnValueRegister = new Register();
            var raxRead = Reg(rax).Read();
            operations.Add(Reg(returnValueRegister).Write(raxRead));
            returnValueLocation = Reg(returnValueRegister).Read();
        }

        // Retrieve values of caller-saved registers
        foreach (var reg in CallerToSave)
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
        var pushRsp = Reg(rsp).Write(rspRead - POINTER_SIZE);
        var rbpRead = Reg(rbp).Read();

        // Allocate slot for old RBP value
        operations.Add(pushRsp);

        // Write down old RBP value
        operations.Add(Mem(rspRead).Write(rbpRead));

        // Set new RBP value
        operations.Add(Reg(rbp).Write(rspRead));

        // Allocate memory for variables
        operations.Add(Reg(rsp).Write(rspRead - _localsOffset));

        // Save and update display entry
        operations.Add(Reg(_oldDisplayValReg).Write(Mem(_displayEntry).Read()));
        operations.Add(Mem(_displayEntry).Write(rbpRead));

        // Write arguments to known locations
        var paramNum = _functionParameters.Count;
        var regParamsNum = Math.Min(paramNum, REG_ARGS_COUNT);
        operations.AddRange(_functionParameters.Zip(ArgumentRegisters).Take(regParamsNum)
            .Select(p => GenerateVariableWrite(p.First, Reg(p.Second).Read())));

        // Callee-saved registers
        foreach (var reg in CalleeToSave)
        {
            var tempReg = _registerToTemporaryMap[reg];
            var regVal = Reg(reg).Read();
            operations.Add(Reg(tempReg).Write(regVal));
        }

        return CodeTreeListToSingleExitList(operations);
    }

    public IReadOnlyList<SingleExitNode> GenerateEpilogue(CodeTreeValueNode? valToReturn)
    {
        var operations = CalleeToSave.Select(reg =>
            Reg(reg).Write(Reg(_registerToTemporaryMap[reg]).Read())).ToList<CodeTreeNode>();

        var rsp = HardwareRegister.RSP;
        var rbp = HardwareRegister.RBP;
        var rax = HardwareRegister.RAX;

        var rspRead = Reg(rsp).Read();

        // Save return value to rax
        if (valToReturn != null)
        {
            operations.Add(Reg(rax).Write(valToReturn));
        }

        // Make main return 0
        if (ParentContext == null)
        {
            operations.Add(Reg(rax).Write(0));
        }

        // Restore old display value
        operations.Add(Mem(_displayEntry).Write(Reg(_oldDisplayValReg).Read()));

        // Free local variables stack space
        operations.Add(Reg(rsp).Write(rspRead + _localsOffset));

        // Retrieve old RBP
        operations.Add(Reg(rbp).Write(Mem(rspRead).Read()));

        // Free RBP slot
        operations.Add(Reg(rsp).Write(rspRead + POINTER_SIZE));

        // Add ret instruction
        operations.Add(new FunctionReturn());

        return CodeTreeListToSingleExitList(operations);
    }

    public bool IsVariableStruct(IFunctionVariable variable) =>
        LocalVariableIsStruct.TryGetValue(variable, out var isStruct)
            ? isStruct
            : _parentContext?.IsVariableStruct(variable)
                ?? throw new ArgumentException("Variable is undefined");

    public CodeTreeValueNode GenerateVariableRead(IFunctionVariable variable)
    {
        var isStruct = IsVariableStruct(variable);
        if (_localVariableLocation.TryGetValue(variable, out var location))
        {
            return isStruct
                ? ((IFunctionContext)this).GetIndirectVariableLocation(variable)
                : location.GenerateRead();
        }
        else
        {
            var indirectLocation = GetParentsIndirectVariableLocation(variable);
            return isStruct
                ? indirectLocation
                : new MemoryRead(indirectLocation);
        }
    }

    public CodeTreeNode GenerateVariableWrite(IFunctionVariable variable, CodeTreeValueNode value)
    {
        if (IsVariableStruct(variable))
        {
            throw new ArgumentException("Variable cannot be a struct");
        }

        return _localVariableLocation.TryGetValue(variable, out var location)
            ? location.GenerateWrite(value)
            : new MemoryWrite(GetParentsIndirectVariableLocation(variable), value);
    }

    CodeTreeValueNode IFunctionContext.GetIndirectVariableLocation(IFunctionVariable variable)
    {
        if (!_localVariableLocation.TryGetValue(variable, out var local))
        {
            // If variable isn't in this context then it should be is the context of some ancestor.
            return ParentContext?.GetIndirectVariableLocation(variable) ??
                   throw new ArgumentException("Variable is undefined");
        }

        if (local is not MemoryLocation localMemory)
        {
            throw new ArgumentException(
                "Variable is not a struct and was added with usedElsewhere=false, so it can't be accessed indirectly",
                nameof(variable));
        }

        return Mem(_displayEntry).Read() - localMemory.Offset;
    }

    public VariableLocation AllocateStackFrameSlot()
    {
        _localsOffset.Value += POINTER_SIZE;
        return new MemoryLocation(_localsOffset.Value);
    }

    private CodeTreeValueNode GetParentsIndirectVariableLocation(IFunctionVariable variable)
    {
        if (ParentContext == null)
        {
            // Get indirect location from ancestors' contexts or throw an error if variable wasn't defined in any context.
            throw new ArgumentException("Variable is undefined");
        }

        return ParentContext.GetIndirectVariableLocation(variable);
    }
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
