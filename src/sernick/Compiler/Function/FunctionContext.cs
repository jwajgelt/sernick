namespace sernick.Compiler.Function;

using ControlFlowGraph.CodeTree;

public sealed class FunctionContext : IFunctionContext
{
    IFunctionContext? _parentContext;
    IReadOnlyCollection<IFunctionParam> _functionParameters;
    bool _valueIsReturned;

    // Maps accesses to registers/memory
    Dictionary<IFunctionVariable, CodeTreeNode> _localVariableLocation;
    int _localsCount;
    CodeTreeNode _displayEntry;

    public FunctionContext(
        IFunctionContext? parent, 
        IReadOnlyCollection<IFunctionParam> parameters, 
        bool returnsValue,
        int contextId
        )
    {
        _localVariableLocation = new Dictionary<IFunctionVariable, CodeTreeNode>();
        _parentContext = parent;
        _functionParameters = parameters;
        _valueIsReturned = returnsValue;
        _localsCount = 0;
        var offsetInDisplay = new Constant(new RegisterValue(contextId));
        // This is of course wrong - we do not know display address right now
        // placeholder
        var displayAddress = new Constant(new RegisterValue(0));
        _displayEntry = new BinaryOperationNode(BinaryOperation.Add, displayAddress, offsetInDisplay);
    }
    public void AddLocal(IFunctionVariable variable, bool usedElsewhere)
    {
        if(usedElsewhere) {
            _localsCount += 1;
            var rbpRead = new RegisterRead(HardwareRegister.RBP);
            var offset = new Constant(new RegisterValue(_localsCount));
            _localVariableLocation.Add(variable, new BinaryOperationNode(BinaryOperation.Add, rbpRead, offset));
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

    public CodeTreeNode GenerateVariableRead(IFunctionVariable variable)
    {
        throw new NotImplementedException();
    }

    public CodeTreeNode GenerateVariableWrite(IFunctionVariable variable, CodeTreeNode value)
    {
        throw new NotImplementedException();
    }
}
