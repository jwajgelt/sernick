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

        var identifiers = new IdentifiersNamespace().RegisterAndMakeVisible(declaration1).RegisterAndMakeVisible(declaration2);

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
        var identifiers = new IdentifiersNamespace().RegisterAndMakeVisible(declaration1);

        Assert.Throws<IdentifiersNamespace.IdentifierCollisionException>(() => identifiers.RegisterAndMakeVisible(declaration2));
    }

    [Fact]
    public void NewScopePreservesIdentifiers()
    {
        var declaration1 = Var("a");

        var identifiers = new IdentifiersNamespace().RegisterAndMakeVisible(declaration1).NewScope();

        Assert.Same(declaration1, identifiers.GetResolution(Ident("a")));
    }

    [Fact]
    public void NewScopeAllowsShadowing()
    {
        var declaration1 = Var("a");
        var declaration2 = Var("a");

        var identifiers = new IdentifiersNamespace().RegisterAndMakeVisible(declaration1).NewScope().RegisterAndMakeVisible(declaration2);

        Assert.Same(declaration2, identifiers.GetResolution(Ident("a")));
    }

    [Fact]
    public void RegisteredDeclarationPreventsAnother()
    {
        var declaration1 = Var("a");
        var declaration2 = Var("a");

        var identifiers = new IdentifiersNamespace().Register(declaration1);

        Assert.Throws<IdentifiersNamespace.IdentifierCollisionException>(() => identifiers.Register(declaration2));
    }
    
    [Fact]
    public void RegisteredDeclarationIsNotVisible()
    {
        var declaration1 = Var("a");

        var identifiers = new IdentifiersNamespace().Register(declaration1);

        Assert.Throws<IdentifiersNamespace.NoSuchIdentifierException>(() => identifiers.GetResolution(Ident("a")));
    }
    
    [Fact]
    public void RegisteredDeclarationDoesNotShadow()
    {
        var declaration1 = Var("a");
        var declaration2 = Var("a");

        var identifiers = new IdentifiersNamespace().RegisterAndMakeVisible(declaration1).NewScope().Register(declaration2);

        Assert.Same(declaration1, identifiers.GetResolution(Ident("a")));
    }

    [Fact]
    public void CannotMakeVisibleUnregistered()
    {
        var declaration = Var("a");
        var identifiers = new IdentifiersNamespace();

        Assert.Throws<ArgumentException>(() => identifiers.MakeVisible(declaration));
    }
}
