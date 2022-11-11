namespace sernick.Ast.Analysis;

using Diagnostics;
using Nodes;

public record NameResolutionError(Identifier Identifier) : IDiagnosticItem
{
    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}

public record MultipleDeclarationsOfTheSameIdentifierError(Declaration Original, Declaration Repeat) : NameResolutionError(Repeat.Name)
{
    public override string ToString()
    {
        return $"Multiple declarations of identifier: {Original}, {Repeat}";
    }
}

public record NotAFunctionError(Identifier Identifier) : NameResolutionError(Identifier)
{
    public override string ToString()
    {
        return $"Identifier does not represent a function: {Identifier}";
    }
}

public record NotAVariableError(Identifier Identifier) : NameResolutionError(Identifier)
{
    public override string ToString()
    {
        return $"Identifier does not represent a variable: {Identifier}";
    }
}

public record UndeclaredIdentifierError(Identifier Identifier) : NameResolutionError(Identifier)
{
    public override string ToString()
    {
        return $"Undeclared identifier: {Identifier}";
    }
}
