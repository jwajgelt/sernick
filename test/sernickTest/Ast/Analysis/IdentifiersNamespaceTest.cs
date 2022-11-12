namespace sernickTest.AST.Analysis;

using sernick.Ast.Analysis.NameResolution;
using sernick.Ast.Nodes;

public class IdentifiersNamespaceTest
{
    [Fact]
    public void IdentifiersAreVisibleAfterAddingThem()
    {
        var declaration1 = GetSimpleDeclaration("a");
        var declaration2 = GetSimpleDeclaration("b");

        var identifiers = new IdentifiersNamespace().Add(declaration1).Add(declaration2);

        Assert.Same(declaration1, identifiers.GetDeclaration(new Identifier("a")));
        Assert.Same(declaration2, identifiers.GetDeclaration(new Identifier("b")));
    }

    [Fact]
    public void NotDeclaredIdentifierThrows()
    {
        var identifiers = new IdentifiersNamespace();

        Assert.Throws<IdentifiersNamespace.NoSuchIdentifierException>(() => identifiers.GetDeclaration(new Identifier("a")));
    }

    [Fact]
    public void DuplicateDeclarationThrows()
    {
        var declaration1 = GetSimpleDeclaration("a");
        var declaration2 = GetSimpleDeclaration("a");
        var identifiers = new IdentifiersNamespace().Add(declaration1);

        Assert.Throws<IdentifiersNamespace.IdentifierCollisionException>(() => identifiers.Add(declaration2));
    }

    [Fact]
    public void NewScopePreservesIdentifiers()
    {
        var declaration1 = GetSimpleDeclaration("a");

        var identifiers = new IdentifiersNamespace().Add(declaration1).NewScope();

        Assert.Same(declaration1, identifiers.GetDeclaration(new Identifier("a")));
    }

    [Fact]
    public void NewScopeAllowsShadowing()
    {
        var declaration1 = GetSimpleDeclaration("a");
        var declaration2 = GetSimpleDeclaration("a");

        var identifiers = new IdentifiersNamespace().Add(declaration1).NewScope().Add(declaration2);

        Assert.Same(declaration2, identifiers.GetDeclaration(new Identifier("a")));
    }

    private static Declaration GetSimpleDeclaration(string name)
    {
        return new VariableDeclaration(new Identifier(name), null, null, false);
    }
}
