namespace sernick.Ast.Analysis.NameResolution;

using System.Collections.Immutable;
using Nodes;

/// <summary>
///     Class responsible for managing identifiers visible from a certain place in code.
/// </summary>
public sealed class IdentifiersNamespace
{
    public sealed class NoSuchIdentifierException : Exception
    {
    }

    public sealed class IdentifierCollisionException : Exception
    {
    }

    private readonly ImmutableHashSet<string> _declaredInCurrentScope;
    private readonly ImmutableDictionary<string, Declaration> _variables;

    public IdentifiersNamespace() : this(ImmutableDictionary<string, Declaration>.Empty,
        ImmutableHashSet<string>.Empty)
    {
    }

    private IdentifiersNamespace(
        ImmutableDictionary<string, Declaration> variables,
        ImmutableHashSet<string> declaredInCurrentScope)
    {
        _variables = variables.WithComparers(null, ReferenceEqualityComparer.Instance);
        _declaredInCurrentScope = declaredInCurrentScope;
    }

    public IdentifiersNamespace RegisterAndMakeVisible(Declaration declaration)
    {
        return Register(declaration).MakeVisible(declaration);
    }

    /// <summary>
    /// Register declaration so that no other declaration with the same identifier cannot appear in this scope.
    /// Does not make the variable visible immediately, because it would allow for "var x = x",
    /// but is needed to make "var x = (var x = 1; x)" invalid.
    /// </summary>
    public IdentifiersNamespace Register(Declaration declaration)
    {
        var name = declaration.Name.Name;
        if (_declaredInCurrentScope.Contains(name))
        {
            throw new IdentifierCollisionException();
        }
        return new IdentifiersNamespace(_variables, _declaredInCurrentScope.Add(name));
    }

    /// <summary>
    /// Make a registered identifier actually refer to the declaration.
    /// </summary>
    public IdentifiersNamespace MakeVisible(Declaration declaration)
    {
        var name = declaration.Name.Name;
        if (!_declaredInCurrentScope.Contains(name))
        {
            throw new ArgumentException("Declaration should have been previously registered.");
        }
        return new IdentifiersNamespace(_variables.SetItem(name, declaration), _declaredInCurrentScope);
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
