namespace sernick.Ast.Analysis.VariableInitialization;

using Nodes;
using Utility;

public static class LocalVariableDeclarations
{
    public static IEnumerable<VariableDeclaration> Process(CodeBlock scope)
    {
        return scope.Accept(new LocalVariableVisitor(), Unit.I);
    }

    private sealed class LocalVariableVisitor : AstVisitor<IEnumerable<VariableDeclaration>, Unit>
    {
        protected override IEnumerable<VariableDeclaration> VisitAstNode(AstNode node, Unit param)
        {
            return node.Children.SelectMany(child => child.Accept(this, param));
        }

        public override IEnumerable<VariableDeclaration> VisitVariableDeclaration(VariableDeclaration node, Unit param)
        {
            return node.Enumerate().Concat(node.InitValue?.Accept(this, param) ?? Enumerable.Empty<VariableDeclaration>());
        }

        public override IEnumerable<VariableDeclaration> VisitFunctionDefinition(FunctionDefinition node, Unit param)
        {
            return Enumerable.Empty<VariableDeclaration>();
        }
    }
}
