#pragma warning disable IDE0052

namespace sernick.Compiler.Function;

using ControlFlowGraph.CodeTree;
using Instruction;
using static ControlFlowGraph.CodeTree.CodeTreeExtensions;

public sealed class FunctionContext : IFunctionContext
{
    private const int PointerSize = 8;
    private readonly IFunctionContext? _parentContext;
    private readonly IReadOnlyCollection<IFunctionParam> _functionParameters;
    private readonly bool _valueIsReturned;

    // Maps accesses to registers/memory
    private readonly Dictionary<IFunctionVariable, VariableLocation> _localVariableLocation;
    private int _localsOffset;
    private CodeTreeValueNode? _displayEntry;
    private readonly int _contextId;

    public FunctionContext(
        IFunctionContext? parent,
        IReadOnlyCollection<IFunctionParam> parameters,
        bool returnsValue,
        int contextId
        )
    {
        _localVariableLocation = new Dictionary<IFunctionVariable, VariableLocation>(ReferenceEqualityComparer.Instance);
        _parentContext = parent;
        _functionParameters = parameters;
        _valueIsReturned = returnsValue;
        _localsOffset = 0;
        _contextId = contextId;

        var fistArgOffset = PointerSize * (1 + _functionParameters.Count);
        var argNum = 0;
        foreach (var param in _functionParameters)
        {
            _localVariableLocation.Add(param, new MemoryLocation(-(fistArgOffset - PointerSize * argNum)));
            argNum += 1;
        }
    }

    public Label Label => "TODO";

    public void AddLocal(IFunctionVariable variable, bool usedElsewhere)
    {
        if (usedElsewhere)
        {
            _localsOffset += PointerSize;
            _localVariableLocation.Add(variable, new MemoryLocation(_localsOffset));
        }
        else
        {
            _localVariableLocation.Add(variable, new RegisterLocation());
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

    public CodeTreeValueNode GenerateVariableRead(IFunctionVariable variable) =>
        _localVariableLocation.TryGetValue(variable, out var location)
            ? location.GenerateRead()
            : new MemoryRead(GetParentsIndirectVariableLocation(variable));

    public CodeTreeNode GenerateVariableWrite(IFunctionVariable variable, CodeTreeValueNode value) =>
        _localVariableLocation.TryGetValue(variable, out var location)
            ? location.GenerateWrite(value)
            : new MemoryWrite(GetParentsIndirectVariableLocation(variable), value);

    public void SetDisplayAddress(CodeTreeValueNode displayAddress)
    {
        _displayEntry = displayAddress + PointerSize * _contextId;
    }

    CodeTreeValueNode IFunctionContext.GetIndirectVariableLocation(IFunctionVariable variable)
    {
        if (!_localVariableLocation.TryGetValue(variable, out var local))
        {
            // If variable isn't in this context then it should be is the context of some ancestor.
            return _parentContext?.GetIndirectVariableLocation(variable) ??
                   throw new ArgumentException("Variable is undefined");
        }

        if (_displayEntry == null)
        {
            throw new Exception("DisplayAddress should be set before generating code");
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
