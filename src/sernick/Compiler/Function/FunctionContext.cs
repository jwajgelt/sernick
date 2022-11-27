#pragma warning disable IDE0052

namespace sernick.Compiler.Function;

using System.Diagnostics.CodeAnalysis;
using ControlFlowGraph.CodeTree;

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
            var offset = new Constant(new RegisterValue(fistArgOffset - PointerSize * argNum));
            _localVariableLocation.Add(param, new MemoryLocation(offset));
            argNum += 1;
        }
    }
    public void AddLocal(IFunctionVariable variable, bool usedElsewhere)
    {
        if (usedElsewhere)
        {
            _localsOffset += PointerSize;
            var offset = new Constant(new RegisterValue(_localsOffset));
            _localVariableLocation.Add(variable, new MemoryLocation(offset));
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

        // Get indirect read from ancestors' contexts or throw an error if variable wasn't defined in any context.
        return _parentContext?.GenerateIndirectVariableRead(variable) ??
               throw new ArgumentException("Variable is undefined");
        ;
    }

    public CodeTreeNode GenerateVariableWrite(IFunctionVariable variable, CodeTreeValueNode value)
    {
        if (_localVariableLocation.TryGetValue(variable, out var location))
        {
            return location.GenerateWrite(value);
        }

        // Get indirect write from ancestors' contexts or throw an error if variable wasn't defined in any context.
        return _parentContext?.GenerateIndirectVariableWrite(variable, value) ??
               throw new ArgumentException("Variable is undefined");
        ;
    }

    public void SetDisplayAddress(CodeTreeValueNode displayAddress)
    {
        var offsetInDisplay = new Constant(new RegisterValue(_contextId));
        _displayEntry = new BinaryOperationNode(BinaryOperation.Add, displayAddress, offsetInDisplay);
    }

    CodeTreeNode IFunctionContext.GenerateIndirectVariableRead(IFunctionVariable variable)
    {
        if (TryGetIndirectLocation(variable, out var memoryLocation))
        {
            return new MemoryRead(memoryLocation);
        }

        return _parentContext?.GenerateIndirectVariableRead(variable) ??
               throw new ArgumentException("Variable is undefined");
    }

    CodeTreeNode IFunctionContext.GenerateIndirectVariableWrite(IFunctionVariable variable, CodeTreeNode value)
    {
        if (TryGetIndirectLocation(variable, out var memoryLocation))
        {
            return new MemoryWrite(memoryLocation, value);
        }

        return _parentContext?.GenerateIndirectVariableWrite(variable, value) ??
               throw new ArgumentException("Variable is undefined");
    }

    /// <summary>
    ///     Returns false if variable doesn't belong to this context.
    ///     Otherwise generates variable location using the display table.
    ///     If the display address hasn't been specified throws an error.
    ///     If variable was added with <code>usedElsewhere = false</code> then throws an error.
    /// </summary>
    private bool TryGetIndirectLocation(IFunctionVariable variable, [MaybeNullWhen(false)] out CodeTreeNode location)
    {
        if (_localVariableLocation.TryGetValue(variable, out var local))
        {
            if (_displayEntry == null)
            {
                throw new Exception("DisplayAddress should be set before generating code");
            }

            if (local is not MemoryLocation localMemory)
            {
                throw new ArgumentException(
                    "Variable wasn't marked with usedElsewhere=true and can't be accessed indirectly",
                    nameof(variable));
            }

            location = new BinaryOperationNode(BinaryOperation.Add, new MemoryRead(_displayEntry), localMemory.Offset);
            return true;
        }

        location = default;
        return false;
    }
}

internal abstract record VariableLocation
{
    public abstract CodeTreeNode GenerateRead();
    public abstract CodeTreeNode GenerateWrite(CodeTreeNode value);
}

internal record MemoryLocation(Constant Offset) : VariableLocation
{
    public override CodeTreeNode GenerateRead() => new MemoryRead(GetDirectLocation());

    public override CodeTreeNode GenerateWrite(CodeTreeNode value) => new MemoryWrite(GetDirectLocation(), value);

    private CodeTreeNode GetDirectLocation() =>
        new BinaryOperationNode(BinaryOperation.Add, new RegisterRead(HardwareRegister.RBP), Offset);
}

internal record RegisterLocation : VariableLocation
{
    private readonly Register _register = new();
    public override CodeTreeNode GenerateRead() =>
        new RegisterRead(_register);

    public override CodeTreeNode GenerateWrite(CodeTreeNode value) =>
        new RegisterWrite(_register, value);
}
