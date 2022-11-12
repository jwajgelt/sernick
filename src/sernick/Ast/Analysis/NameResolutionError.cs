namespace sernick.Ast.Analysis;

using Diagnostics;
using Nodes;

public record NameResolutionError(Identifier Identifier) : IDiagnosticItem
{
    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}

public record MultipleDeclarationsError(Declaration Original, Declaration Repeat) : NameResolutionError(Repeat.Name)
{
    public override string ToString()
    {
        return $"Multiple declarations of identifier: {Original.Name}, locations: {Original.LocationRange.Start}, {Repeat.LocationRange.Start}";
    }
}

public record NotAFunctionError(Identifier Identifier) : NameResolutionError(Identifier)
{
    public override string ToString()
    {
        return $"Identifier does not represent a function: {Identifier.Name}, location: {Identifier.LocationRange.Start}";
    }
}

public record NotAVariableError(Identifier Identifier) : NameResolutionError(Identifier)
{
    public override string ToString()
    {
        return $"Identifier does not represent a variable: {Identifier.Name}, location: {Identifier.LocationRange.Start}";
    }
}

public record UndeclaredIdentifierError(Identifier Identifier) : NameResolutionError(Identifier)
{
    public override string ToString()
    {
        return $"Undeclared identifier: {Identifier.Name}, location: {Identifier.LocationRange.Start}";
    }
}
