namespace sernick.Compiler.Function;
using ControlFlowGraph.CodeTree;

public abstract record FunctionVariable;

public interface IFunctionContext : IFunctionCaller
{
    // Do we need this?
    public void AddLocal(FunctionVariable variable, bool usedElsewhere);

    public RegisterWrite ResultVariable { get; set; }

    public IReadOnlyList<CodeTreeNode> GeneratePrologue();

    public IReadOnlyList<CodeTreeNode> GenerateEpilogue();

    public CodeTreeNode GenerateRegisterRead(CodeTreeNode variable, bool direct);

    public CodeTreeNode GenerateRegisterWrite(CodeTreeNode variable, CodeTreeNode value, bool direct);

    public CodeTreeNode GenerateMemoryRead(CodeTreeNode variable, bool direct);

    public CodeTreeNode GenerateMemoryWrite(CodeTreeNode variable, CodeTreeNode value, bool direct);
}
