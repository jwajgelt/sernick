namespace sernick.Compiler.Function;
using ControlFlowGraph.CodeTree;

public abstract record FunctionVariable;

public interface IFunctionContext : IFunctionCaller
{
    public void AddLocal(FunctionVariable variable, bool usedElsewhere);

    public RegisterWrite? ResultVariable { get; set; }

    public IReadOnlyList<CodeTreeNode> GeneratePrologue();

    public IReadOnlyList<CodeTreeNode> GenerateEpilogue();

    /// <summary>
    ///     Generates either memory read or register read
    ///     depending on whether the variable is stored on the stack or in registers.
    ///     This should only be used with local variables in the owner function's scope.
    /// </summary>
    /// <param name="variable">a variable declared earlier as local</param>
    public CodeTreeNode GenerateVariableRead(FunctionVariable variable);

    /// <summary>
    ///     Generates either memory write or register write
    ///     depending on whether the variable is stored on the stack or in registers.
    ///     This should only be used in the owner function's scope.
    /// </summary>
    /// <param name="variable">a variable declared earlier as local</param>
    public CodeTreeNode GenerateVariableWrite(FunctionVariable variable, CodeTreeNode value);

    /// <summary>
    ///     Generates memory read using the Display Table.
    ///     This can be used outside of the owner function's scope.
    /// </summary>
    /// <param name="variable">a variable declared earlier as local</param>
    public CodeTreeNode GenerateIndirectVariableRead(FunctionVariable variable);

    /// <summary>
    ///     Generates memory write using the Display Table.
    ///     This can be used outside of the owner function's scope.
    /// </summary>
    /// <param name="variable">a variable declared earlier as local</param>
    public CodeTreeNode GenerateIndirectVariableWrite(FunctionVariable variable, CodeTreeNode value);
}
