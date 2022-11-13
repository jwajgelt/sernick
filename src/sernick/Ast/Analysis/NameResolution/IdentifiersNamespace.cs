namespace sernick.Ast.Analysis.NameResolution;

using System.Collections.Immutable;
using Nodes;

/// <summary>
///     Class responsible for managing identifiers visible from a certain place in code.
/// </summary>
public sealed class IdentifiersNamespace
{
    public class NoSuchIdentifierException : Exception
    {
    }

    public class IdentifierCollisionException : Exception
    {
    }

    private readonly ImmutableHashSet<string> _currentScope;
    private readonly ImmutableDictionary<string, Declaration> _variables;

    public IdentifiersNamespace() : this(ImmutableDictionary<string, Declaration>.Empty,
        ImmutableHashSet<string>.Empty)
    {
    }

    private IdentifiersNamespace(ImmutableDictionary<string, Declaration> variables,
        ImmutableHashSet<string> currentScope) =>
        (_variables, _currentScope) = (variables, currentScope);

    public IdentifiersNamespace Add(Declaration declaration)
    {
        var name = declaration.Name.Name;
        if (_currentScope.Contains(name))
        {
            throw new IdentifierCollisionException();
        }

        return new IdentifiersNamespace(_variables.Remove(name).Add(name, declaration), _currentScope.Add(name));
    }

    public Declaration GetDeclaration(Identifier identifier)
    {
        var name = identifier.Name;
        if (_variables.TryGetValue(name, out var declaration))
        {
            return declaration;
        }

        throw new NoSuchIdentifierException();
    }

    public IdentifiersNamespace NewScope()
    {
        return new IdentifiersNamespace(_variables, ImmutableHashSet<string>.Empty);
    }
}
