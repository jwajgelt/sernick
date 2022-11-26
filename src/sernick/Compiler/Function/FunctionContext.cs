namespace sernick.Compiler.Function;

using ControlFlowGraph.CodeTree;

public sealed class FunctionContext : IFunctionContext
{
    private readonly IFunctionContext? _parentContext;
    private readonly IReadOnlyCollection<IFunctionParam> _functionParameters;
    private readonly bool _valueIsReturned;

    // Maps accesses to registers/memory
    private readonly Dictionary<IFunctionVariable, CodeTreeNode> _localVariableLocation;
    private int _localsCount;
    private readonly CodeTreeNode _displayEntry;

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
        _displayEntry = new BinaryOperationNode(BinaryOperation.Sub, displayAddress, offsetInDisplay);
    }
    public void AddLocal(IFunctionVariable variable, bool usedElsewhere)
    {
        if (usedElsewhere)
        {
            _localsCount += 1;
            var rbpRead = new RegisterRead(HardwareRegister.RBP);
            var offset = new Constant(new RegisterValue(_localsCount));
            _localVariableLocation.Add(variable, new BinaryOperationNode(BinaryOperation.Sub, rbpRead, offset));
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
