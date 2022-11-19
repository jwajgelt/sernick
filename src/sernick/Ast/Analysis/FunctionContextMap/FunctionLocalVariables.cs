namespace sernick.Ast.Analysis.FunctionContextMap;

using Nodes;

/// <summary>
/// Class representing internal state of <see cref="FunctionContextMapProcessor"/> AST visitor
/// </summary>
public sealed class FunctionLocalVariables
{
    private readonly Dictionary<FunctionDefinition, HashSet<Declaration>> _locals =
        new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<Declaration, HashSet<FunctionDefinition>> _functions =
        new(ReferenceEqualityComparer.Instance);

    public void EnterFunction(FunctionDefinition funcDefinition)
    {
        _locals[funcDefinition] = new HashSet<Declaration>(ReferenceEqualityComparer.Instance);
    }

    public void ExitFunction(FunctionDefinition funcDefinition) => _locals.Remove(funcDefinition);

    /// <summary>
    /// Precondition: <c>EnterFunction(func)</c> was called before, but <c>ExitFunction(func)</c> wasn't yet
    /// </summary>
    public void DeclareLocal(Declaration local, FunctionDefinition func)
    {
        _locals[func].Add(local);
        _functions[local] = new HashSet<FunctionDefinition>(ReferenceEqualityComparer.Instance) { func };
    }

    /// <summary>
    /// Precondition: <c>DeclareLocal(local)</c> was called before
    /// </summary>
    public void UseLocal(Declaration local, FunctionDefinition func)
    {
        _functions[local].Add(func);
    }

    public void DiscardLocal(Declaration local) => _functions.Remove(local);

    public IEnumerable<Variable> this[FunctionDefinition funcDefinition] =>
        _locals[funcDefinition].Select(var => new Variable(var, _functions[var]));

    public record struct Variable(Declaration Declaration,
        IEnumerable<FunctionDefinition> ReferencingFunctions);
}
