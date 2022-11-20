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

    private readonly ImmutableDictionary<string, Declaration> _declaredInCurrentScope;
    private readonly ImmutableDictionary<string, Declaration> _resolutions;

    public IdentifiersNamespace() : this(ImmutableDictionary<string, Declaration>.Empty,
        ImmutableDictionary<string, Declaration>.Empty)
    {
    }

    private IdentifiersNamespace(
        ImmutableDictionary<string, Declaration> resolutions,
        ImmutableDictionary<string, Declaration> declaredInCurrentScope)
    {
        _resolutions = resolutions.WithComparers(null, ReferenceEqualityComparer.Instance);
        _declaredInCurrentScope = declaredInCurrentScope.WithComparers(null, ReferenceEqualityComparer.Instance);
    }

    public IdentifiersNamespace RegisterAndMakeVisible(Declaration declaration)
    {
        return Register(declaration).MakeVisible(declaration);
    }

    /// <summary>
    /// Register declaration so that no other declaration with the same identifier can appear in this scope.
    /// Does not make the variable visible immediately, because it would allow for "var x = x",
    /// but is needed to make "var x = (var x = 1; x)" invalid.
    /// </summary>
    public IdentifiersNamespace Register(Declaration declaration)
    {
        var name = declaration.Name.Name;
        if (_declaredInCurrentScope.ContainsKey(name))
        {
            throw new IdentifierCollisionException();
        }

        return new IdentifiersNamespace(_resolutions, _declaredInCurrentScope.Add(name, declaration));
    }

    /// <summary>
    /// Make an identifier resolve to a previously registered declaration.
    /// Throws if declaration was not registered.
    /// </summary>
    public IdentifiersNamespace MakeVisible(Declaration declaration)
    {
        var name = declaration.Name.Name;
        if (_declaredInCurrentScope.TryGetValue(name, out var value) && ReferenceEquals(value, declaration))
        {
            return new IdentifiersNamespace(_resolutions.SetItem(name, declaration), _declaredInCurrentScope);
        }

        throw new ArgumentException("Declaration should have been previously registered.");
    }

    /// <summary>
    /// Returns declaration bound to the identifier.
    /// </summary>
    public Declaration GetResolution(Identifier identifier)
    {
        var name = identifier.Name;
        if (_resolutions.TryGetValue(name, out var declaration))
        {
            return declaration;
        }

        throw new NoSuchIdentifierException();
    }

    /// <summary>
    /// Returns declaration from the current scope.
    /// This is different from GetResolution(identifier), because some declarations might have not yet been made visible.
    /// </summary>
    public Declaration GetDeclaredInThisScope(Identifier identifier)
    {
        var name = identifier.Name;
        if (_declaredInCurrentScope.TryGetValue(name, out var declaration))
        {
            return declaration;
        }

        throw new NoSuchIdentifierException();
    }

    public IdentifiersNamespace NewScope()
    {
        return new IdentifiersNamespace(_resolutions, ImmutableDictionary<string, Declaration>.Empty);
    }
}
