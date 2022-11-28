#pragma warning disable IDE0052

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

    private const int PointerSize = 8;
    private readonly IFunctionContext? _parentContext;
    private readonly IReadOnlyCollection<IFunctionParam> _functionParameters;
    private readonly bool _valueIsReturned;

    // Maps accesses to registers/memory
    private readonly Dictionary<IFunctionVariable, CodeTreeValueNode> _localVariableLocation;
    private int _localsOffset;
    private CodeTreeValueNode? _displayEntry;
    private readonly Register _oldDisplayValReg;
    private readonly Dictionary<HardwareRegister, Register> _registerToTemporaryMap;
    public int Depth { get; }
    public FunctionContext(
        IFunctionContext? parent,
        IReadOnlyCollection<IFunctionParam> parameters,
        bool returnsValue
        )
    {
        _localVariableLocation = new Dictionary<IFunctionVariable, CodeTreeValueNode>(ReferenceEqualityComparer.Instance);
        _parentContext = parent;
        _functionParameters = parameters;
        _valueIsReturned = returnsValue;
        _localsOffset = 0;
        _registerToTemporaryMap = calleeToSave.ToDictionary<HardwareRegister, HardwareRegister, Register>(reg => reg, _ => new Register(), ReferenceEqualityComparer.Instance);
        _oldDisplayValReg = new Register();

        Depth = (parent?.Depth ?? -1) + 1;

        var fistArgOffset = PointerSize * (1 + _functionParameters.Count);
        var argNum = 0;
        foreach (var param in _functionParameters)
        {
            _localVariableLocation.Add(param, fistArgOffset - PointerSize * argNum);
            argNum += 1;
        }
    }
    public void AddLocal(IFunctionVariable variable, bool usedElsewhere)
    {
        if (usedElsewhere)
        {
            _localsOffset += PointerSize;
            _localVariableLocation.Add(variable, _localsOffset);
        }
        else
        {
            _localVariableLocation.Add(variable, new RegisterRead(new Register()));
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

        // Put args onto stack
        foreach (var arg in arguments)
        {
            operations.Add(pushRsp);
            operations.Add(Mem(rspRead).Write(arg));
        }

        // Performing actual call (puts return addess on stack and jumps)
        operations.Add(new FunctionCall(this));

        // Remove arguments from stack (we already returned from call)
        operations.Add(Reg(rsp).Write(rspRead + PointerSize * arguments.Count));

        // If value is returned, then put it from RAX to virtual register
        CodeTreeNode? returnValueLocation = null;
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

        Register rsp = HardwareRegister.RSP;
        Register rbp = HardwareRegister.RBP;

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
        if (_displayEntry == null)
        {
            throw new Exception("DisplayAddress should be set before generating code");
        }

        operations.Add(Reg(_oldDisplayValReg).Write(Mem(_displayEntry).Read()));
        operations.Add(Mem(_displayEntry).Write(rbpRead));

        // Allocate memory for variables
        operations.Add(Reg(rsp).Write(rspRead - _localsOffset));

        // Callee-saved registers
        foreach (var reg in calleeToSave)
        {
            var tempReg = _registerToTemporaryMap[reg];
            var regVal = Reg(reg).Read();
            operations.Add(Reg(tempReg).Write(regVal));
        }

        return CodeTreeListToSingleExitList(operations);
    }

    public IReadOnlyList<SingleExitNode> GenerateEpilogue()
    {
        var operations = calleeToSave.Select(reg =>
            Reg(reg).Write(Reg(_registerToTemporaryMap[reg]).Read())).ToList<CodeTreeNode>();

        var rsp = HardwareRegister.RSP;
        var rbp = HardwareRegister.RBP;

        var rspRead = Reg(rsp).Read();

        // Free local variables stack space
        operations.Add(Reg(rsp).Write(rspRead + _localsOffset));

        // Retrieve old RBP
        operations.Add(Reg(rbp).Write(Mem(rspRead).Read()));

        // Free RBP slot
        operations.Add(Reg(rsp).Write(rspRead + PointerSize));

        // Restore old display value
        if (_displayEntry == null)
        {
            throw new Exception("DisplayAddress should be set before generating code");
        }

        operations.Add(Mem(_displayEntry).Write(Reg(_oldDisplayValReg).Read()));

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

    public CodeTreeValueNode GenerateVariableRead(IFunctionVariable variable)
    {
        throw new NotImplementedException();
    }

    public CodeTreeNode GenerateVariableWrite(IFunctionVariable variable, CodeTreeValueNode value)
    {
        throw new NotImplementedException();
    }

    public void SetDisplayAddress(CodeTreeValueNode displayAddress)
    {
        _displayEntry = displayAddress + PointerSize * Depth;
    }
}
