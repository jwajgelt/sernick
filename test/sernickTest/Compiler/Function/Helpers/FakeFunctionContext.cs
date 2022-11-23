namespace sernickTest.Compiler.Function.Helpers;

using sernick.Compiler.Function;
using sernick.ControlFlowGraph.CodeTree;

public sealed class FakeFunctionContext : IFunctionContext
{
    private readonly Dictionary<FunctionVariable, bool> _locals = new();

    public IReadOnlyDictionary<FunctionVariable, bool> Locals => _locals;

    public void AddLocal(FunctionVariable variable, bool usedElsewhere) => _locals[variable] = usedElsewhere;
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

    public (IReadOnlyList<CodeTreeNode> codeGraph, CodeTreeNode? resultLocation) GenerateCall(IReadOnlyList<CodeTreeNode> arguments)
    {
        throw new NotImplementedException();
    }
}
