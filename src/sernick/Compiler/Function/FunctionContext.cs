#pragma warning disable IDE0052

namespace sernick.Compiler.Function;

using ControlFlowGraph.CodeTree;

public sealed class FunctionContext : IFunctionContext
{
    private static readonly HardwareRegister[] registersToSave = {
        HardwareRegister.R12,
        HardwareRegister.R13,
        HardwareRegister.R14,
        HardwareRegister.R15,
        HardwareRegister.RBX, 
    };

    private static readonly CodeTreeNode slotSize = new Constant(new RegisterValue(PointerSize));

    private const int PointerSize = 8;
    private readonly IFunctionContext? _parentContext;
    private readonly IReadOnlyCollection<IFunctionParam> _functionParameters;
    private readonly bool _valueIsReturned;

    // Maps accesses to registers/memory
    private readonly Dictionary<IFunctionVariable, CodeTreeNode> _localVariableLocation;
    private int _localsOffset;
    private CodeTreeNode? _displayEntry;
    private readonly int _contextId;

    private Dictionary<HardwareRegister, Register> _registerToTemporaryMap;

    public FunctionContext(
        IFunctionContext? parent,
        IReadOnlyCollection<IFunctionParam> parameters,
        bool returnsValue,
        int contextId
        )
    {
        _localVariableLocation = new Dictionary<IFunctionVariable, CodeTreeNode>(ReferenceEqualityComparer.Instance);
        _parentContext = parent;
        _functionParameters = parameters;
        _valueIsReturned = returnsValue;
        _localsOffset = 0;
        _contextId = contextId;
        _registerToTemporaryMap = new Dictionary<HardwareRegister, Register>(ReferenceEqualityComparer.Instance);

        foreach(HardwareRegister reg in registersToSave) 
        {
            _registerToTemporaryMap.Add(reg, new Register());
        }

        var fistArgOffset = PointerSize * (1 + _functionParameters.Count);
        var argNum = 0;
        foreach (var param in _functionParameters)
        {
            _localVariableLocation.Add(param, new Constant(new RegisterValue(fistArgOffset - PointerSize * argNum)));
            argNum += 1;
        }
    }
    public void AddLocal(IFunctionVariable variable, bool usedElsewhere)
    {
        if (usedElsewhere)
        {
            _localsOffset += PointerSize;
            _localVariableLocation.Add(variable, new Constant(new RegisterValue(_localsOffset)));
        }
        else
        {
            _localVariableLocation.Add(variable, new RegisterRead(new Register()));
        }
    }

    public IFunctionCaller.GenerateCallResult GenerateCall(IReadOnlyList<CodeTreeNode> arguments)
    {
        List<CodeTreeNode> operations = new List<CodeTreeNode>();

        HardwareRegister RSP = HardwareRegister.RSP;
        HardwareRegister RBP = HardwareRegister.RBP;

        var rspRead = new RegisterRead(RSP);
        var decrementedRsp = new BinaryOperationNode(BinaryOperation.Sub, rspRead, slotSize);
        var pushRsp = new RegisterWrite(RSP, decrementedRsp);

        var rbpRead = new RegisterRead(RBP);

        // Put args onto stack
        foreach(CodeTreeNode arg in arguments)
        {
            operations.Add(pushRsp);
            operations.Add(new MemoryWrite(rspRead, arg));
        }

        // Generate the call (so )
        operations.Add(pushRsp);
    }

    private CodeTreeRoot? cfgFromList(IReadOnlyList<CodeTreeNode> operations, CodeTreeRoot? nextRoot)
    {
        CodeTreeRoot? lastRoot = nextRoot;
        foreach(CodeTreeNode op in operations)
        {
            lastRoot = new SingleExitNode(lastRoot, op);
        }

        return lastRoot;
    }

    public RegisterWrite? ResultVariable { get; set; }

    public IReadOnlyList<CodeTreeNode> GeneratePrologue()
    {
        List<CodeTreeNode> operations = new List<CodeTreeNode>();

        

        // Allocate slot for old RSP value
        operations.Add(pushRsp);

        // Write down old RBP value
        operations.Add(new MemoryWrite(rspRead, rbpRead));

        // Set new RBP value
        operations.Add(new RegisterWrite(RBP, rspRead));

        // Allocate memory for variables
        var varOffsetConst = new Constant(new RegisterValue(_localsOffset));
        newRspVal = new BinaryOperationNode(BinaryOperation.Sub, rspRead, varOffsetConst);
        operations.Add(new RegisterWrite(RSP, newRspVal));

        // Calee-saved registers
        foreach(HardwareRegister reg in registersToSave)
        {
            var tempReg = _registerToTemporaryMap[reg];
            var regVal = new RegisterRead(reg);
            operations.Add(new RegisterWrite(tempReg, regVal));
        }

        return operations;
    }

    public IReadOnlyList<CodeTreeNode> GenerateEpilogue()
    {
        List<CodeTreeNode> operations = new List<CodeTreeNode>();
        
        // Retrieve values of callee-saved registers
        foreach(HardwareRegister reg in registersToSave)
        {
            var tempReg = _registerToTemporaryMap[reg];
            var tempVal = new RegisterRead(tempReg);
            operations.Add(new RegisterWrite(reg, tempVal));
        }

        HardwareRegister RSP = HardwareRegister.RSP;

        // Free local variables stack space
        var varOffsetConst = new Constant(new RegisterValue(_localsOffset));
        var rspVal = new RegisterRead(RSP);
        var newRspVal = new BinaryOperationNode(BinaryOperation.Add, rspVal, varOffsetConst);
        operations.Add(new RegisterWrite(RSP, newRspVal));

        return operations;
    }

    public CodeTreeNode GenerateVariableRead(IFunctionVariable variable)
    {
        throw new NotImplementedException();
    }

    public CodeTreeNode GenerateVariableWrite(IFunctionVariable variable, CodeTreeNode value)
    {
        throw new NotImplementedException();
    }

    public void SetDisplayAddress(CodeTreeNode displayAddress)
    {
        var offsetInDisplay = new Constant(new RegisterValue(_contextId));
        _displayEntry = new BinaryOperationNode(BinaryOperation.Add, displayAddress, offsetInDisplay);
    }
}
