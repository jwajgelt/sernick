namespace sernick.Ast.Analysis;

using Diagnostics;
using Input;
using Nodes;

public sealed record NameResolutionError(Identifier Identifier, ILocation Location) : IDiagnosticItem
{
    public override string ToString()
    {
        return $"Name resolution error: cannot resolve symbol \"{Identifier.Name}\" at {Location}";
    }

    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;

    public bool Equals(NameResolutionError? other) => other is not null && ToString() == other.ToString();
    public override int GetHashCode() => (Identifier.Name, Location).GetHashCode();
}
