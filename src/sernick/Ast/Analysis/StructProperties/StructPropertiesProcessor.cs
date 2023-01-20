namespace sernick.Ast.Analysis.StructProperties;

using NameResolution;
using Nodes;
using sernick.Diagnostics;
using Utility;
using static Compiler.PlatformConstants;

public record struct StructProperties(
    IReadOnlyDictionary<StructDeclaration, int> StructSizesDeclarations,
    IReadOnlyDictionary<Identifier, int> StructSizes,
    IReadOnlyDictionary<FieldDeclaration, int> FieldOffsets,
    IReadOnlyDictionary<FieldDeclaration, int> FieldSizes)
{
    public StructProperties() : this(
        new Dictionary<StructDeclaration, int>(ReferenceEqualityComparer.Instance),
        new Dictionary<Identifier, int>(ReferenceEqualityComparer.Instance),
        new Dictionary<FieldDeclaration, int>(ReferenceEqualityComparer.Instance),
        new Dictionary<FieldDeclaration, int>(ReferenceEqualityComparer.Instance)
    )
    { }

    public StructProperties JoinWith(StructProperties other)
    {
        return new StructProperties(
            StructSizesDeclarations.JoinWith(other.StructSizesDeclarations, ReferenceEqualityComparer.Instance),
            StructSizes.JoinWith(other.StructSizes, ReferenceEqualityComparer.Instance),
            FieldOffsets.JoinWith(other.FieldOffsets, ReferenceEqualityComparer.Instance),
            FieldSizes.JoinWith(other.FieldSizes, ReferenceEqualityComparer.Instance)
        );
    }
};

public static class StructPropertiesProcessor
{
    public static StructProperties Process(AstNode ast, NameResolutionResult nameResolution, IDiagnostics diagnostics)
    {
        var visitor = new StructPropertiesVisitor(nameResolution.StructDeclarations, diagnostics);
        var result = visitor.VisitAstTree(ast, new());
        var structSizes = nameResolution.StructDeclarations
            .ToDictionary(kv => kv.Key, kv => result.StructSizesDeclarations[kv.Value],
                ReferenceEqualityComparer.Instance as IEqualityComparer<Identifier>);

        return result with { StructSizes = structSizes };
    }

    private sealed class StructPropertiesVisitor : AstVisitor<StructProperties, StructProperties>
    {
        private readonly IReadOnlyDictionary<Identifier, StructDeclaration> _structDeclarations;
        private readonly IDiagnostics _diagnostics;

        public StructPropertiesVisitor(IReadOnlyDictionary<Identifier, StructDeclaration> structDeclarations, IDiagnostics diagnostics)
        {
            _structDeclarations = structDeclarations;
            _diagnostics = diagnostics;
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
            var structSizes = new Dictionary<Identifier, int>(ReferenceEqualityComparer.Instance);
            var fieldOffsets = new Dictionary<FieldDeclaration, int>(ReferenceEqualityComparer.Instance);
            var fieldSizes = new Dictionary<FieldDeclaration, int>(ReferenceEqualityComparer.Instance);

            var offset = 0;
            foreach (var field in node.Fields)
            {
                fieldOffsets[field] = offset;
                if (field.Type is StructType type)
                {
                    var fieldTypeDeclaration = _structDeclarations[type.Struct];
                    if (!currentResult.StructSizesDeclarations.TryGetValue(fieldTypeDeclaration, out var structSize))
                    {
                        _diagnostics.Report(new StructPropertiesProcessorError(field.Name.Name, type));
                    }

                    fieldSizes[field] = structSize;
                    offset += structSize;
                }
                else
                {
                    fieldSizes[field] = POINTER_SIZE;
                    offset += POINTER_SIZE;
                }
            }

            return new(new Dictionary<StructDeclaration, int> { { node, offset } }, structSizes, fieldOffsets, fieldSizes);
        }
    }
}
