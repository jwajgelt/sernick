namespace sernickTest.Ast.Analysis;

using sernick.Ast.Analysis.NameResolution;
using sernick.Ast.Nodes;
using sernick.Input;
using sernick.Utility;
using Tokenizer.Lexer.Helpers;

public class IdentifiersNamespaceTest
{
    private static readonly Range<ILocation> loc = new(new FakeInput.Location(0), new FakeInput.Location(0));

    [Fact]
    public void IdentifiersAreVisibleAfterAddingThem()
    {
        var declaration1 = GetSimpleDeclaration("a");
        var declaration2 = GetSimpleDeclaration("b");

        var identifiers = new IdentifiersNamespace().Add(declaration1).Add(declaration2);

        Assert.Same(declaration1, identifiers.GetDeclaration(GetSimpleIdentifier("a")));
        Assert.Same(declaration2, identifiers.GetDeclaration(GetSimpleIdentifier("b")));
    }

    [Fact]
    public void NotDeclaredIdentifierThrows()
    {
        var identifiers = new IdentifiersNamespace();

        Assert.Throws<IdentifiersNamespace.NoSuchIdentifierException>(() => identifiers.GetDeclaration(GetSimpleIdentifier("a")));
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

        Assert.Same(declaration1, identifiers.GetDeclaration(GetSimpleIdentifier("a")));
    }

    [Fact]
    public void NewScopeAllowsShadowing()
    {
        var declaration1 = GetSimpleDeclaration("a");
        var declaration2 = GetSimpleDeclaration("a");

        var identifiers = new IdentifiersNamespace().Add(declaration1).NewScope().Add(declaration2);

        Assert.Same(declaration2, identifiers.GetDeclaration(GetSimpleIdentifier("a")));
    }

    private static Identifier GetSimpleIdentifier(string name)
    {
        return new Identifier(name, loc);
    }

    private static Declaration GetSimpleDeclaration(string name)
    {
        return new VariableDeclaration(GetSimpleIdentifier(name), null, null, false, loc);
    }
}
