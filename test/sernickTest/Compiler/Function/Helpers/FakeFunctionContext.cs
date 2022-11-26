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

    public IFunctionCaller.GenerateCallResult GenerateCall(IReadOnlyList<CodeTreeNode> arguments)
    {
        throw new NotImplementedException();
    }
}
