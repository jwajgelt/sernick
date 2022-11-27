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
        throw new NotImplementedException();
    }

    public RegisterWrite? ResultVariable { get; set; }

    public IReadOnlyList<CodeTreeNode> GeneratePrologue()
    {
        List<CodeTreeNode> operations = new List<CodeTreeNode>();
        foreach(HardwareRegister reg in registersToSave)
        {
            var tempReg = _registerToTemporaryMap[reg];
            var regVal = new RegisterRead(reg);
            operations.Add(new RegisterWrite(tempReg, regVal));
        }

        HardwareRegister RSP = HardwareRegister.RSP;
        HardwareRegister RBP = HardwareRegister.RBP;

        // Allocate memory for arguments
        var argsOffsetConst = new Constant(new RegisterValue(PointerSize * _functionParameters.Count));
        var rspVal = new RegisterRead(RSP);
        var newRsp = new BinaryOperationNode(BinaryOperation.Sub, rspVal, argsOffsetConst);
        operations.Add(new RegisterWrite(RSP, newRsp));

        // Allocate slot for return address
        rspVal = new RegisterRead(RSP);
        newRsp = new BinaryOperationNode(BinaryOperation.Sub, rspVal, slotSize);
        operations.Add(new RegisterWrite(RSP, newRsp));

        // Set RA

        // Allocate slot for old RSP value
        rspVal = new RegisterRead(RSP);
        newRsp = new BinaryOperationNode(BinaryOperation.Sub, rspVal, slotSize);
        operations.Add(new RegisterWrite(RSP, newRsp));

        // Set old RSP value

        return operations;
    }

    public IReadOnlyList<CodeTreeNode> GenerateEpilogue()
    {
        List<CodeTreeNode> operations = new List<CodeTreeNode>();
        
        
        foreach(HardwareRegister reg in registersToSave)
        {
            var tempReg = _registerToTemporaryMap[reg];
            var tempVal = new RegisterRead(tempReg);
            operations.Add(new RegisterWrite(reg, tempVal));
        }

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
