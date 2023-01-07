namespace sernick.Ast.Analysis;

using Diagnostics;
using Nodes;

public abstract record NameResolutionError(Identifier Identifier) : IDiagnosticItem
{
    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}

public sealed record MultipleDeclarationsError(Declaration Original, Declaration Repeat) : NameResolutionError(Repeat.Name)
{
    public override string ToString()
    {
        return $"Multiple declarations of identifier: {Original.Name}, locations: {Original.LocationRange.Start}, {Repeat.LocationRange.Start}";
    }

    public bool Equals(MultipleDeclarationsError? other) => other is not null && ToString() == other.ToString();
    public override int GetHashCode() => ToString().GetHashCode();
}

public sealed record NotAFunctionError(Identifier Identifier) : NameResolutionError(Identifier)
{
    public override string ToString()
    {
        return $"Identifier does not represent a function: {Identifier.Name}, location: {Identifier.LocationRange.Start}";
    }

    public bool Equals(NotAFunctionError? other) => other is not null && ToString() == other.ToString();
    public override int GetHashCode() => ToString().GetHashCode();
}

public sealed record NotAVariableError(Identifier Identifier) : NameResolutionError(Identifier)
{
    public override string ToString()
    {
        return $"Identifier does not represent a variable: {Identifier.Name}, location: {Identifier.LocationRange.Start}";
    }

    public bool Equals(NotAVariableError? other) => other is not null && ToString() == other.ToString();
    public override int GetHashCode() => ToString().GetHashCode();
}

public sealed record NotATypeError(Identifier Identifier) : NameResolutionError(Identifier)
{
    public override string ToString()
    {
        return $"Identifier does not represent a struct type: {Identifier.Name}, location: {Identifier.LocationRange.Start}";
    }

    public bool Equals(NotAVariableError? other) => other is not null && ToString() == other.ToString();
    public override int GetHashCode() => ToString().GetHashCode();
}

public sealed record UndeclaredIdentifierError(Identifier Identifier) : NameResolutionError(Identifier)
{
    public override string ToString()
    {
        return $"Undeclared identifier: {Identifier.Name}, location: {Identifier.LocationRange.Start}";
    }

    public bool Equals(UndeclaredIdentifierError? other) => other is not null && ToString() == other.ToString();
    public override int GetHashCode() => ToString().GetHashCode();
}
