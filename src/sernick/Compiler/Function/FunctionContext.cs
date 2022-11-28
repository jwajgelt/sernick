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
            operations.Add(new MemoryWrite(rspRead, arg));
        }

        // Performing actual call (puts return addess on stack and jumps)
        operations.Add(new FunctionCall(this));

        // Remove arguments from stack (we already returned from call)
        operations.Add(new RegisterWrite(rsp, rspRead + PointerSize * arguments.Count));

        // If value is returned, then put it from RAX to virtual register
        CodeTreeNode? returnValueLocation = null;
        if (_valueIsReturned)
        {
            var returnValueRegister = new Register();
            var raxRead = new RegisterRead(rax);
            operations.Add(new RegisterWrite(returnValueRegister, raxRead));
            returnValueLocation = new RegisterRead(returnValueRegister);
        }

        // Retrieve values of caller-saved registers
        foreach (var reg in calleeToSave)
        {
            var tempReg = callerSavedMap[reg];
            var tempVal = new RegisterRead(tempReg);
            operations.Add(new RegisterWrite(reg, tempVal));
        }

        return new IFunctionCaller.GenerateCallResult(CodeTreeListToSingleExitList(operations), returnValueLocation);
    }

    public IReadOnlyList<SingleExitNode> GeneratePrologue()
    {
        var operations = new List<CodeTreeNode>();

        Register rsp = HardwareRegister.RSP;
        Register rbp = HardwareRegister.RBP;

        var rspRead = new RegisterRead(rsp);
        var pushRsp = new RegisterWrite(rsp, rspRead - PointerSize);
        var rbpRead = new RegisterRead(rbp);

        // Allocate slot for old RBP value
        operations.Add(pushRsp);

        // Write down old RBP value
        operations.Add(new MemoryWrite(rspRead, rbpRead));

        // Set new RBP value
        operations.Add(new RegisterWrite(rbp, rspRead));

        // Save and update display entry
        if (_displayEntry == null)
        {
            throw new Exception("DisplayAddress should be set before generating code");
        }

        operations.Add(new RegisterWrite(_oldDisplayValReg, new MemoryRead(_displayEntry)));
        operations.Add(new MemoryWrite(_displayEntry, rbpRead));

        // Allocate memory for variables
        operations.Add(new RegisterWrite(rsp, rspRead - _localsOffset));

        // Callee-saved registers
        foreach (var reg in calleeToSave)
        {
            var tempReg = _registerToTemporaryMap[reg];
            var regVal = new RegisterRead(reg);
            operations.Add(new RegisterWrite(tempReg, regVal));
        }

        return CodeTreeListToSingleExitList(operations);
    }

    public IReadOnlyList<SingleExitNode> GenerateEpilogue()
    {
        var operations = calleeToSave.Select(reg =>
            Reg(reg).Write(Reg(_registerToTemporaryMap[reg]).Read())).ToList<CodeTreeNode>();

        var rsp = HardwareRegister.RSP;
        var rbp = HardwareRegister.RBP;

        var rspRead = new RegisterRead(rsp);

        // Free local variables stack space
        operations.Add(new RegisterWrite(rsp, rspRead + _localsOffset));

        // Retrieve old RBP
        operations.Add(new RegisterWrite(rbp, new MemoryRead(rspRead)));

        // Free RBP slot
        operations.Add(new RegisterWrite(rsp, rspRead + PointerSize));

        // Restore old display value
        if (_displayEntry == null)
        {
            throw new Exception("DisplayAddress should be set before generating code");
        }

        operations.Add(new MemoryWrite(_displayEntry, new RegisterRead(_oldDisplayValReg)));

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
