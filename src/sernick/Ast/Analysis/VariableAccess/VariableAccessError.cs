namespace sernick.Ast.Analysis.VariableAccess;

using Diagnostics;
using Nodes;

public abstract record VariableAccessError() : IDiagnosticItem
{
    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
}

public sealed record InnerFunctionConstVariableWriteError(
    FunctionDefinition DeclaringFunction,
    FunctionDefinition AssigningFunction,
    Assignment Assignment) : VariableAccessError
{
    public override string ToString()
    {
        var declaringFunctionString = DeclaringFunction.Name.Name == "" ? $"the Main function" : $"function {DeclaringFunction.Name.Name}";
        return $"Const variable {Assignment.Left.Name} declared in {declaringFunctionString}" +
               $" but assigned in function {AssigningFunction.Name.Name}, location: {Assignment.LocationRange.Start}";
    }
}
