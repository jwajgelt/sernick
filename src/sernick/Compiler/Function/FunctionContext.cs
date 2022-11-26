#pragma warning disable IDE0052

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
    private CodeTreeNode? _displayEntry;
    private readonly int _contextId;

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
        _contextId = contextId;

        int fistArgOffset = 4 + 4 * _functionParameters.Count();
        int argNum = 0;
        foreach(IFunctionParam param in _functionParameters)
        {
            argNum += 1;
            var rbpRead = new RegisterRead(HardwareRegister.RBP);
            var offset = new Constant(new RegisterValue(fistArgOffset - 4*(argNum-1)));
            _localVariableLocation.Add(param, new BinaryOperationNode(BinaryOperation.Add, rbpRead, offset));
        }
    }
    public void AddLocal(IFunctionVariable variable, bool usedElsewhere)
    {
        if (usedElsewhere)
        {
            _localsCount += 4;
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

    public void SetDisplayAddress(CodeTreeNode displayAddress)
    {
        var offsetInDisplay = new Constant(new RegisterValue(_contextId));
        _displayEntry = new BinaryOperationNode(BinaryOperation.Add, displayAddress, offsetInDisplay);
    }
}
