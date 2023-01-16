namespace sernick.ControlFlowGraph.CodeTree;

using Ast;
using Ast.Analysis.NameResolution;
using Ast.Analysis.StructProperties;
using Ast.Nodes;
using Utility;
using static CodeTreeExtensions;
using static Compiler.PlatformConstants;

public class StructHelper
{
    private readonly StructProperties _properties;
    private readonly NameResolutionResult _nameResolution;

    public static IEnumerable<CodeTreeNode> GenerateStructCopy(CodeTreeValueNode targetStruct, CodeTreeValueNode sourceStruct,
        int structSize)
    {
        return Enumerable.Range(0, structSize / POINTER_SIZE)
            .Select(offset =>
                Mem(targetStruct + POINTER_SIZE * offset)
                    .Write(Mem(sourceStruct + POINTER_SIZE * offset).Read()));
    }

    public StructHelper(StructProperties properties, NameResolutionResult nameResolution)
    {
        _properties = properties;
        _nameResolution = nameResolution;
    }

    public CodeTreeValueNode GenerateStructFieldRead(
        CodeTreeValueNode sourceStruct,
        FieldDeclaration field)
    {
        var offset = _properties.FieldOffsets[field];
        var source = sourceStruct + offset;

        if (field.Type is not StructType)
        {
            return Mem(source).Read();
        }

        return source;
    }

    public IEnumerable<CodeTreeNode> GenerateStructFieldWrite(
        CodeTreeValueNode targetStruct,
        CodeTreeValueNode source,
        FieldDeclaration field)
    {
        var offset = _properties.FieldOffsets[field];
        var target = targetStruct + offset;

        if (field.Type is not StructType)
        {
            return Mem(target).Write(source).Enumerate();
        }

        var fieldSize = _properties.FieldSizes[field];
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

    public int GetStructFieldSize(StructType type, Identifier fieldName)
    {
        var field = GetStructFieldDeclaration(type, fieldName);
        return _properties.FieldSizes[field];
    }
}
