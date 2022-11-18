namespace sernick.Ast.Analysis.FunctionContextMap;

using Nodes;

/// <summary>
/// Class representing internal state of <see cref="FunctionContextMapProcessor"/> AST visitor
/// </summary>
public sealed class FunctionLocalVariables
{
    private readonly Dictionary<FunctionDefinition, HashSet<VariableDeclaration>> _locals =
        new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<VariableDeclaration, HashSet<FunctionDefinition>> _functions =
        new(ReferenceEqualityComparer.Instance);

    public void EnterFunction(FunctionDefinition funcDefinition)
    {
        _locals[funcDefinition] = new HashSet<VariableDeclaration>(ReferenceEqualityComparer.Instance);
    }

    public void ExitFunction(FunctionDefinition funcDefinition) => _locals.Remove(funcDefinition);

    /// <summary>
    /// Precondition: <c>EnterFunction(func)</c> was called before, but <c>ExitFunction(func)</c> wasn't yet
    /// </summary>
    public void DeclareLocal(VariableDeclaration local, FunctionDefinition func)
    {
        _locals[func].Add(local);
        _functions[local] = new HashSet<FunctionDefinition>(ReferenceEqualityComparer.Instance) { func };
    }

    /// <summary>
    /// Precondition: <c>DeclareLocal(local)</c> was called before
    /// </summary>
    public void UseLocal(VariableDeclaration local, FunctionDefinition func)
    {
        _functions[local].Add(func);
    }

    public IEnumerable<Variable> this[FunctionDefinition funcDefinition] =>
        _locals[funcDefinition]
            .Select(var => new Variable(var, _functions[var]));

    public record struct Variable(VariableDeclaration Declaration,
        IEnumerable<FunctionDefinition> ReferencingFunctions);
}
