namespace sernick.Compiler.Function;
using ControlFlowGraph.CodeTree;

public interface IFunctionContext : IFunctionCaller
{
    public void AddLocal(IFunctionVariable variable, bool usedElsewhere);

    public IReadOnlyList<SingleExitNode> GeneratePrologue();

    public IReadOnlyList<SingleExitNode> GenerateEpilogue();

    public int Depth { get; }

    /// <summary>
    ///     If variable is local then generates either memory read or register read
    ///     depending on whether the variable is stored on the stack or in registers.
    ///     <br/>
    ///     If variable isn't in the function's scope then generates memory read
    ///     using the Display Table.
    /// </summary>
    public CodeTreeValueNode GenerateVariableRead(IFunctionVariable variable);

    /// <summary>
    ///     If variable is local then generates either memory write or register write
    ///     depending on whether the variable is stored on the stack or in registers.
    ///     <br/>
    ///     If variable isn't in the function's scope then generates memory write
    ///     using the Display Table.
    /// </summary>
    public CodeTreeNode GenerateVariableWrite(IFunctionVariable variable, CodeTreeValueNode value);

    protected internal CodeTreeValueNode GetIndirectVariableLocation(IFunctionVariable variable);
}
