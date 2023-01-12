namespace sernick.Ast.Analysis;

using NameResolution;
using Nodes;

public record struct StructProperties(
    IReadOnlyDictionary<StructDeclaration, int> StructSizes,
    IReadOnlyDictionary<FieldDeclaration, int> FieldOffsets
)
{ };

public static class StructPropertiesProcessor
{
    public static StructProperties Process(AstNode ast, NameResolutionResult nameResolution)
    {
        throw new NotImplementedException();
    }

    private sealed class StructPropertiesVisitor : AstVisitor<StructProperties, StructDeclaration?>
    {
        private readonly IReadOnlyDictionary<Identifier, StructDeclaration> _structDeclarations;

        public StructPropertiesVisitor(IReadOnlyDictionary<Identifier, StructDeclaration> structDeclarations)
        {
            _structDeclarations = structDeclarations;
        }

        protected override StructProperties VisitAstNode(AstNode node, StructDeclaration? parent)
        {
            throw new NotImplementedException();
        }
    }
}
