namespace sernick.Ast.Analysis.NameResolution;

using System.Collections.Immutable;
using Diagnostics;
using Nodes;

/// <summary>
///     Class responsible for managing identifiers visible from a certain place in code.
/// </summary>
public sealed class IdentifiersNamespace2
{
    public class NoSuchIdentifierException : Exception
    {
    }

    public class IdentifierCollisionException : Exception
    {
    }

    private readonly ImmutableHashSet<string> _currentScope;
    private readonly ImmutableDictionary<string, Declaration> _variables;

    public IdentifiersNamespace2() : this(ImmutableDictionary<string, Declaration>.Empty,
        ImmutableHashSet<string>.Empty)
    {
    }

    private IdentifiersNamespace2(ImmutableDictionary<string, Declaration> variables,
        ImmutableHashSet<string> currentScope) =>
        (_variables, _currentScope) = (variables, currentScope);

    public IdentifiersNamespace2 Add(Declaration declaration)
    {
        var name = declaration.Name.Name;
        if (_currentScope.Contains(name))
        {
            throw new IdentifierCollisionException();
        }
        return new IdentifiersNamespace2(_variables.Remove(name).Add(name, declaration), _currentScope.Add(name));
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
    
    public IdentifiersNamespace2 NewScope()
    {
        return new IdentifiersNamespace2(_variables, ImmutableHashSet<string>.Empty);
    }
}
