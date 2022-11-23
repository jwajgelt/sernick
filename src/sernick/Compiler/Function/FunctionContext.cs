namespace sernick.Compiler.Function;

using ControlFlowGraph.CodeTree;

public sealed class FunctionContext : IFunctionContext
{
    public void AddLocal(FunctionVariable variable, bool usedElsewhere)
    {
        // silently ignore for now
    }

    public (IReadOnlyList<CodeTreeNode> codeGraph, CodeTreeNode? resultLocation) GenerateCall(IReadOnlyList<CodeTreeNode> arguments)
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
