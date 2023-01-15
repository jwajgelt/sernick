namespace sernick.ControlFlowGraph.CodeTree;

using Ast;
using Ast.Analysis.NameResolution;
using Ast.Analysis.StructProperties;
using Ast.Nodes;
using Utility;
using static CodeTreeExtensions;

public class StructHelper
{
    private readonly StructProperties _properties;
    private readonly NameResolutionResult _nameResolution;

    public static IEnumerable<CodeTreeNode> GenerateStructCopy(CodeTreeValueNode targetStruct, CodeTreeValueNode sourceStruct,
        int structSize)
    {
        return Enumerable.Range(0, structSize)
            .Select(offset =>
                Mem(targetStruct + offset)
                    .Write(Mem(sourceStruct + offset).Read()));
    }

    public StructHelper(StructProperties properties, NameResolutionResult nameResolution)
    {
        _properties = properties;
        _nameResolution = nameResolution;
    }

    public IEnumerable<CodeTreeNode> GenerateStructFieldRead(
        CodeTreeValueNode sourceStruct,
        FieldDeclaration field)
    {
        var offset = _properties.FieldOffsets[field];
        var source = sourceStruct + offset;

        if (field.Type is not StructType)
        {
            return Mem(source).Read().Enumerate();
        }

        return source.Enumerate();

    }

    public IEnumerable<CodeTreeNode> GenerateStructFieldWrite(
        CodeTreeValueNode targetStruct,
        CodeTreeValueNode source,
        FieldDeclaration field,
        StructDeclaration targetStructDeclaration)
    {
        var structSize = _properties.StructSizes[targetStructDeclaration];
        var offset = _properties.FieldOffsets[field];
        var target = targetStruct + offset;

        if (field.Type is not StructType)
        {
            return Mem(target).Write(source).Enumerate();
        }

        var fieldSize = _properties.FieldOffsets.Values.Where(fieldOffset => fieldOffset > offset).Concat(structSize.Enumerate()).Min() - offset;
        return GenerateStructCopy(target, source, fieldSize);
    }

    public int GetStructTypeSize(StructType type)
    {
        var structDeclaration = _nameResolution.StructDeclarations[type.Struct];
        return _properties.StructSizes[structDeclaration];
    }

    public FieldDeclaration GetStructFieldDeclaration(StructType type, Identifier fieldName)
    {
        var structDeclaration = _nameResolution.StructDeclarations[type.Struct];
        foreach (var field in structDeclaration.Fields)
        {
            if (field.Name.Name.Equals(fieldName.Name))
            {
                return field;
            }
        }

        throw new NotSupportedException(
            $"Invalid field {fieldName.Name} access on struct of type {type}");
    }
}
