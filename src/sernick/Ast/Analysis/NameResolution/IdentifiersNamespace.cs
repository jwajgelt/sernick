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

    private readonly ImmutableDictionary<string, Declaration> _resolutions;
    private readonly ImmutableHashSet<string> _declaredInCurrentScope;

    public IdentifiersNamespace() : this(ImmutableDictionary<string, Declaration>.Empty,
        ImmutableHashSet<string>.Empty)
    {
    }

    private IdentifiersNamespace(
        ImmutableDictionary<string, Declaration> resolutions,
        ImmutableHashSet<string> declaredInCurrentScope)
    {
        _resolutions = resolutions.WithComparers(null, ReferenceEqualityComparer.Instance);
        _declaredInCurrentScope = declaredInCurrentScope;
    }

    public IdentifiersNamespace Add(Declaration declaration)
    {
        var name = declaration.Name.Name;
        if (_declaredInCurrentScope.Contains(name))
        {
            throw new IdentifierCollisionException();
        }

        return new IdentifiersNamespace(_resolutions.SetItem(name, declaration), _declaredInCurrentScope.Add(name));
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

    public IdentifiersNamespace NewScope()
    {
        return new IdentifiersNamespace(_resolutions, ImmutableHashSet<string>.Empty);
    }
}
