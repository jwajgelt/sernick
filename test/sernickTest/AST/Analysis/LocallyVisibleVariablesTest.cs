namespace sernickTest.AST.Analysis;

using Moq;
using sernick.Ast;
using sernick.Ast.Analysis;
using sernick.Ast.Nodes;

public class LocallyVisibleVariablesTest
{
    [Fact]
    public void AddedVariableIsAccessible()
    {
        var declaration = new VariableDeclaration(new Identifier("x"), new IntType(), null, false);
        var localVariables = new NameResolutionLocallyVisibleVariables();

        var newLocalVariables = localVariables.Add(declaration);
        
        Assert.Same(declaration, newLocalVariables.Variables["x"]);
    }
    
    [Fact]
    public void AddedVariableShadowsOldVariable()
    {
        var oldDeclaration = new VariableDeclaration(new Identifier("x"), new IntType(), null, false);
        var localVariables = new NameResolutionLocallyVisibleVariables().Add(oldDeclaration);
        var newDeclaration = new VariableDeclaration(new Identifier("x"), new IntType(), null, false);
        
        var newLocalVariables = localVariables.Add(newDeclaration);
        
        Assert.Same(newDeclaration, newLocalVariables.Variables["x"]);
    }
}
