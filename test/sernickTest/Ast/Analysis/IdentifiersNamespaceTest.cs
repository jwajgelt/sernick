namespace sernickTest.AST.Analysis;

using sernick.Ast;
using sernick.Ast.Analysis.NameResolution;
using sernick.Ast.Nodes;

public class IdentifiersNamespaceTest
{
    [Fact]
    public void IdentifiersAreVisibleAfterAddingThem()
    {
        var declaration1 = GetSimpleDeclaration("a");
        var declaration2 = GetSimpleDeclaration("b");

        var identifiers = new IdentifiersNamespace2().Add(declaration1).Add(declaration2);
        
        Assert.Same(declaration1, identifiers.GetDeclaration(new Identifier("a")));
        Assert.Same(declaration2, identifiers.GetDeclaration(new Identifier("b")));
    }
    
    [Fact]
    public void NotDeclaredIdentifierThrows()
    {
        var identifiers = new IdentifiersNamespace2();
        
        Assert.Throws<IdentifiersNamespace2.NoSuchIdentifierException>(() => identifiers.GetDeclaration(new Identifier("a")));
    }
    
    [Fact]
    public void DuplicateDeclarationThrows()
    {
        var declaration1 = GetSimpleDeclaration("a");
        var declaration2 = GetSimpleDeclaration("a");
        var identifiers = new IdentifiersNamespace2().Add(declaration1);

        Assert.Throws<IdentifiersNamespace2.IdentifierCollisionException>(() => identifiers.Add(declaration2));
    }
    
    [Fact]
    public void NewScopePreservesIdentifiers()
    {
        var declaration1 = GetSimpleDeclaration("a");
        
        var identifiers = new IdentifiersNamespace2().Add(declaration1).NewScope();
        
        Assert.Same(declaration1, identifiers.GetDeclaration(new Identifier("a")));
    }

    [Fact]
    public void NewScopeAllowsShadowing()
    {
        var declaration1 = GetSimpleDeclaration("a");
        var declaration2 = GetSimpleDeclaration("a");
        
        var identifiers = new IdentifiersNamespace2().Add(declaration1).NewScope().Add(declaration2);
        
        Assert.Same(declaration2, identifiers.GetDeclaration(new Identifier("a")));
    }
    
    private static Declaration GetSimpleDeclaration(string name)
    {
        return new VariableDeclaration(new Identifier(name), null, null, false);
    }

    private static VariableDeclaration GetVariable(string name)
    {
        return new VariableDeclaration(new Identifier(name), null, null, false);
    }

    private static FunctionParameterDeclaration GetParameter(string name)
    {
        return new FunctionParameterDeclaration(new Identifier(name), new IntType(), null);
    }

    private static FunctionDefinition GetFunction(string name)
    {
        return new FunctionDefinition(
            new Identifier(name),
            Enumerable.Empty<FunctionParameterDeclaration>(),
            new IntType(),
            new CodeBlock(new ReturnStatement(new IntLiteralValue(0)))
        );
    }
}
