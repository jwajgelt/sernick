namespace sernick.Ast.Analysis.VariableAccess;

using Diagnostics;
using Nodes;

public abstract record VariableAccessError() : IDiagnosticItem
{
    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}

public sealed record InnerFunctionConstVariableWriteError(
    FunctionDefinition DeclaringFunction,
    VariableDeclaration Declaration,
    FunctionDefinition AssigningFunction,
    VariableValue Value) : VariableAccessError
{
    public override string ToString()
    {
        var declaringFunctionString = DeclaringFunction.Name.Name == "" ? $"the Main function" : $"function {DeclaringFunction.Name.Name}";
        return $"Const variable {Value.Identifier} declared in {declaringFunctionString} (location: {Declaration.LocationRange.Start})" +
               $" but assigned in function {AssigningFunction.Name.Name}, location: {Value.LocationRange.Start}";
    }
    public bool Equals(InnerFunctionConstVariableWriteError? other) => other is not null && ToString() == other.ToString();
    public override int GetHashCode() => ToString().GetHashCode();
}
