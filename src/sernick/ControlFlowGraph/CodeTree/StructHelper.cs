namespace sernick.ControlFlowGraph.CodeTree;

using Ast;
using Ast.Analysis.StructProperties;
using Ast.Nodes;
using Utility;
using static CodeTreeExtensions;
using static Compiler.PlatformConstants;

public class StructHelper
{
    private readonly StructProperties _properties;

    public static IEnumerable<CodeTreeNode> GenerateStructCopy(CodeTreeValueNode targetStruct, CodeTreeValueNode sourceStruct,
        int structSize)
    {
        return Enumerable.Range(0, structSize)
            .Select(offset =>
                Mem(targetStruct + offset)
                    .Write(Mem(sourceStruct + offset).Read()));
    }

    public StructHelper(StructProperties properties)
    {
        _properties = properties;
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
}
