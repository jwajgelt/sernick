namespace sernick.Compiler.Function;

using ControlFlowGraph.CodeTree;

public sealed class FunctionContext : IFunctionContext
{
    public void AddLocal(FunctionVariable variable, bool usedElsewhere)
    {
        // silently ignore for now
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

    public CodeTreeNode GenerateVariableRead(FunctionVariable variable)
    {
        throw new NotImplementedException();
    }

    public CodeTreeNode GenerateVariableWrite(FunctionVariable variable, CodeTreeNode value)
    {
        throw new NotImplementedException();
    }

    public CodeTreeNode GenerateIndirectVariableRead(FunctionVariable variable)
    {
        throw new NotImplementedException();
    }

    public CodeTreeNode GenerateIndirectVariableWrite(FunctionVariable variable, CodeTreeNode value)
    {
        throw new NotImplementedException();
    }
}
