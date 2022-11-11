namespace sernickTest.AST.Analysis;

using Moq;
using sernick.Ast;
using sernick.Ast.Analysis.NameResolution;
using sernick.Ast.Nodes;
using sernick.Diagnostics;

public class LocallyVisibleVariablesTest
{
    [Fact]
    public void AddedVariableIsAccessible()
    {
        var declaration = new VariableDeclaration(new Identifier("x"), new IntType(), null, false);
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        var localVariables = new LocalVariablesManager(diagnostics.Object);

        var newLocalVariables = localVariables.Add(declaration);
        
        Assert.Same(declaration, newLocalVariables.Variables["x"]);
    }
    
    [Fact]
    public void AddedVariableShadowsOldVariable()
    {
        var oldDeclaration = new VariableDeclaration(new Identifier("x"), new IntType(), null, false);
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        var localVariables = new LocalVariablesManager(diagnostics.Object).Add(oldDeclaration);
        var newDeclaration = new VariableDeclaration(new Identifier("x"), new IntType(), null, false);
        
        var newLocalVariables = localVariables.Add(newDeclaration);
        
        Assert.Same(newDeclaration, newLocalVariables.Variables["x"]);
    }
}
