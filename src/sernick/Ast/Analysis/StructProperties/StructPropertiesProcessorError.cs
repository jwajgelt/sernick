namespace sernick.Ast.Analysis.StructProperties;

using sernick.Diagnostics;

public record StructPropertiesProcessorError(string FieldName, Type Type) : IDiagnosticItem
{
    public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;

    public override string ToString()
    {
        return $"Struct size not found for \"{FieldName} : {Type}\" (referencing itself)";
    }
}
