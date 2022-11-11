namespace sernick.Ast.Analysis;

using Diagnostics;
using Input;
using Nodes;

public sealed record NameResolutionError(Identifier Identifier, ILocation Location) : IDiagnosticItem
{
    public bool Equals(IDiagnosticItem? other) => other is NameResolutionError && other.ToString() == ToString();

    public override string ToString()
    {
        return $"Name resolution error: cannot resolve symbol \"{Identifier.Name}\" at {Location}";
    }

    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}
