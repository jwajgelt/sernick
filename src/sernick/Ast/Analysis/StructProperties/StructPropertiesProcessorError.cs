namespace sernick.Ast.Analysis.StructProperties;

using sernick.Diagnostics;

public record StructPropertiesProcessorError(string fieldName, Type type) : IDiagnosticItem
{
    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;

    public override string ToString()
    {
        return $"Struct size not found for \"{fieldName} : {type}\" (referencing itself)";
    }
}