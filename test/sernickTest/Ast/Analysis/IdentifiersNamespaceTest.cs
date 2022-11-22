namespace sernickTest.Ast.Analysis;

using sernick.Ast.Analysis.NameResolution;
using static Helpers.AstNodesExtensions;

public class IdentifiersNamespaceTest
{
    [Fact]
    public void IdentifiersAreVisibleAfterAddingThem()
    {
        var declaration1 = Var("a");
        var declaration2 = Var("b");

        var identifiers = new IdentifiersNamespace().Add(declaration1).Add(declaration2);

        Assert.Same(declaration1, identifiers.GetResolution(Ident("a")));
        Assert.Same(declaration2, identifiers.GetResolution(Ident("b")));
    }

    [Fact]
    public void NotDeclaredIdentifierThrows()
    {
        var identifiers = new IdentifiersNamespace();

        Assert.Throws<IdentifiersNamespace.NoSuchIdentifierException>(() => identifiers.GetResolution(Ident("a")));
    }

    [Fact]
    public void DuplicateDeclarationThrows()
    {
        var declaration1 = Var("a");
        var declaration2 = Var("a");
        var identifiers = new IdentifiersNamespace().Add(declaration1);

        Assert.Throws<IdentifiersNamespace.IdentifierCollisionException>(() => identifiers.Add(declaration2));
    }

    [Fact]
    public void NewScopePreservesIdentifiers()
    {
        var declaration1 = Var("a");

        var identifiers = new IdentifiersNamespace().Add(declaration1).NewScope();

        Assert.Same(declaration1, identifiers.GetResolution(Ident("a")));
    }

    [Fact]
    public void NewScopeAllowsShadowing()
    {
        var declaration1 = Var("a");
        var declaration2 = Var("a");

        var identifiers = new IdentifiersNamespace().Add(declaration1).NewScope().Add(declaration2);

        Assert.Same(declaration2, identifiers.GetResolution(Ident("a")));
    }
}
