namespace sernick.Compiler.Function;

using ControlFlowGraph.CodeTree;

public sealed class FunctionContext : IFunctionContext
{
    List<(FunctionVariable, bool)> _localVariables;
    IFunctionContext? _parentContext;
    IReadOnlyCollection<FunctionParam> _functionParameters;
    bool _valueIsReturned;

    // Maps accesses to registers/memory
    Dictionary<FunctionVariable, CodeTreeNode> _localVariableLocation;
    int _localsCount;
    // Since display has to be represented by a global array
    // we will just allocate a virtual register, which will
    // need to be mapped to the display array later
    Register displayEntry;

    public FunctionContext(
        IFunctionContext? parent, 
        IReadOnlyCollection<FunctionParam> parameters, 
        bool returnsValue 
        )
    {
        _localVariables = new List<(FunctionVariable, bool)>();
        _localVariableLocation = new Dictionary<FunctionVariable, CodeTreeNode>();
        _parentContext = parent;
        _functionParameters = parameters;
        _valueIsReturned = returnsValue;
        _localsCount = 0;
    }
    public void AddLocal(FunctionVariable variable, bool usedElsewhere)
    {
        if(usedElsewhere) {
            _localsCount += 1;
            var rbpRead = new RegisterRead(HardwareRegister.RBP);
            var offset = new Constant(new RegisterValue(_localsCount));
            _localVariableLocation.Add(variable, new BinaryOperation());
        } else {
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
        throw new NotImplementedException();
    }

    public IReadOnlyList<CodeTreeNode> GenerateEpilogue()
    {
        throw new NotImplementedException();
    }

    public CodeTreeNode GenerateRegisterRead(CodeTreeNode variable, bool direct)
    {
        throw new NotImplementedException();
    }

    public CodeTreeNode GenerateRegisterWrite(CodeTreeNode variable, CodeTreeNode value, bool direct)
    {
        throw new NotImplementedException();
    }

    public CodeTreeNode GenerateMemoryRead(CodeTreeNode variable, bool direct)
    {
        throw new NotImplementedException();
    }

    public CodeTreeNode GenerateMemoryWrite(CodeTreeNode variable, CodeTreeNode value, bool direct)
    {
        throw new NotImplementedException();
    }
}
