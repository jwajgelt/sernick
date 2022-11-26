namespace sernickTest.Compiler.Function.Helpers;

using Castle.Core;
using sernick.Compiler.Function;
using sernick.ControlFlowGraph.CodeTree;

public sealed class FakeFunctionContext : IFunctionContext
{
    private readonly Dictionary<IFunctionVariable, bool> _locals = new(ReferenceEqualityComparer<IFunctionVariable>.Instance);

    public IReadOnlyDictionary<IFunctionVariable, bool> Locals => _locals;

    public void AddLocal(IFunctionVariable variable, bool usedElsewhere) => _locals[variable] = usedElsewhere;
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

    public IFunctionCaller.GenerateCallResult GenerateCall(IReadOnlyList<CodeTreeNode> arguments)
    {
        throw new NotImplementedException();
    }
}
