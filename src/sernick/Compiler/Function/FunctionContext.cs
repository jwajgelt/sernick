#pragma warning disable IDE0052

namespace sernick.Compiler.Function;

using ControlFlowGraph.CodeTree;
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

    public CodeTreeValueNode GenerateVariableRead(IFunctionVariable variable)
    {
        if (_localVariableLocation.TryGetValue(variable, out var location))
        {
            return location.GenerateRead();
        }

        // Get indirect location from ancestors' contexts or throw an error if variable wasn't defined in any context.
        var indirectLocation = _parentContext?.GetIndirectVariableLocation(variable) ??
                               throw new ArgumentException("Variable is undefined");

        return new MemoryRead(indirectLocation);
    }

    public CodeTreeNode GenerateVariableWrite(IFunctionVariable variable, CodeTreeValueNode value)
    {
        if (_localVariableLocation.TryGetValue(variable, out var location))
        {
            return location.GenerateWrite(value);
        }

        // Get indirect location from ancestors' contexts or throw an error if variable wasn't defined in any context.
        var indirectLocation = _parentContext?.GetIndirectVariableLocation(variable) ??
                               throw new ArgumentException("Variable is undefined");

        return new MemoryWrite(indirectLocation, value);
    }

    public void SetDisplayAddress(CodeTreeValueNode displayAddress)
    {
        _displayEntry = displayAddress + _contextId;
    }

    CodeTreeNode IFunctionContext.GetIndirectVariableLocation(IFunctionVariable variable)
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
}

internal abstract record VariableLocation
{
    public abstract CodeTreeNode GenerateRead();
    public abstract CodeTreeNode GenerateWrite(CodeTreeNode value);
}

internal record MemoryLocation(CodeTreeNode Offset) : VariableLocation
{
    public override CodeTreeNode GenerateRead() => new MemoryRead(GetDirectLocation());

    public override CodeTreeNode GenerateWrite(CodeTreeNode value) => new MemoryWrite(GetDirectLocation(), value);

    private CodeTreeNode GetDirectLocation() => Reg(HardwareRegister.RBP).Read() - Offset;
}

internal record RegisterLocation : VariableLocation
{
    private readonly Register _register = new();
    public override CodeTreeNode GenerateRead() =>
        new RegisterRead(_register);

    public override CodeTreeNode GenerateWrite(CodeTreeNode value) =>
        new RegisterWrite(_register, value);
}
