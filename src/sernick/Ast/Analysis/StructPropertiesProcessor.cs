namespace sernick.Ast.Analysis;

using NameResolution;
using Nodes;
using Utility;
using static Compiler.PlatformConstants;

public record struct StructProperties(
    IReadOnlyDictionary<StructDeclaration, int> StructSizes,
    IReadOnlyDictionary<FieldDeclaration, int> FieldOffsets
)
{
    public StructProperties() : this(
        new Dictionary<StructDeclaration, int>(ReferenceEqualityComparer.Instance),
        new Dictionary<FieldDeclaration, int>(ReferenceEqualityComparer.Instance)
    ) { }

    public StructProperties JoinWith(StructProperties other)
    {
        return new StructProperties(
            StructSizes.JoinWith(other.StructSizes, ReferenceEqualityComparer.Instance),
            FieldOffsets.JoinWith(other.FieldOffsets, ReferenceEqualityComparer.Instance)
        );
    }
};

public static class StructPropertiesProcessor
{
    public static StructProperties Process(AstNode ast, NameResolutionResult nameResolution)
    {
        var visitor = new StructPropertiesVisitor(nameResolution.StructDeclarations);
        return visitor.VisitAstTree(ast, new());
    }

    private sealed class StructPropertiesVisitor : AstVisitor<StructProperties, StructProperties>
    {
        private readonly IReadOnlyDictionary<Identifier, StructDeclaration> _structDeclarations;

        public StructPropertiesVisitor(IReadOnlyDictionary<Identifier, StructDeclaration> structDeclarations)
        {
            _structDeclarations = structDeclarations;
        }

        protected override StructProperties VisitAstNode(AstNode node, StructProperties currentResult)
        {
            return node.Children.Aggregate(
                new StructProperties(),
                (result, next) =>
                {
                    var childResult = next.Accept(this, currentResult.JoinWith(result));
                    return result.JoinWith(childResult);
                }
            );
        }

        public override StructProperties VisitStructDeclaration(StructDeclaration node, StructProperties currentResult)
        {
            var fieldOffsets = new Dictionary<FieldDeclaration, int>(ReferenceEqualityComparer.Instance);

            int offset = 0;
            foreach (FieldDeclaration field in node.Fields)
            {
                fieldOffsets[field] = offset;
                if (field.Type is StructType type)
                {
                    if (!_structDeclarations.TryGetValue(type.Struct, out var fieldTypeDeclaration))
                    {
                        throw new Exception($"Struct declaration not found in name resolution for \"{field.Name.Name} : {type.Struct.Name}\"");
                    }
                    if (!currentResult.StructSizes.TryGetValue(fieldTypeDeclaration, out var structSize))
                    {
                        throw new Exception($"Struct size not found for \"{field.Name.Name} : {type.Struct.Name}\" (referencing itself)");
                    }
                    offset += structSize;
                }
                else
                {
                    offset += POINTER_SIZE;
                }
            }
            return new(new Dictionary<StructDeclaration, int>{ { node, offset } }, fieldOffsets);
        }
    }
}
