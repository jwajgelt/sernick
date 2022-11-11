namespace sernickTest.AST.Analysis;

using Moq;
using sernick.Ast;
using sernick.Ast.Analysis;
using sernick.Ast.Analysis.NameResolution;
using sernick.Ast.Nodes;
using sernick.Diagnostics;

public class LocalVariableManagerTest
{
    [Fact]
    public void IdentifiersAreVisibleAfterAddingThem()
    {
        var variable = GetVariable("a");
        var parameter = GetParameter("b");
        var function = GetFunction("c");
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        var manager = new LocalVariablesManager(diagnostics.Object);

        var finalManager = manager.Add(variable).Add(parameter).Add(function);

        Assert.Same(variable, finalManager.GetAssignedVariableDeclaration(new Identifier("a")));
        Assert.Same(variable, finalManager.GetUsedVariableDeclaration(new Identifier("a")));
        Assert.Same(parameter, finalManager.GetUsedVariableDeclaration(new Identifier("b")));
        Assert.Same(function, finalManager.GetCalledFunctionDeclaration(new Identifier("c")));
    }

    [Fact]
    public void NotDeclaredIdentifiersAreReported()
    {
        var diagnostics = new Mock<IDiagnostics>();
        var manager = new LocalVariablesManager(diagnostics.Object);

        manager.GetAssignedVariableDeclaration(new Identifier("a"));
        manager.GetUsedVariableDeclaration(new Identifier("a"));
        manager.GetCalledFunctionDeclaration(new Identifier("a"));

        diagnostics.Verify(d => d.Report(It.IsAny<UndeclaredIdentifierError>()), Times.Exactly(3));
    }

    [Fact]
    public void MultipleDeclarationsAreReported()
    {
        var firstDeclaration = GetFunction("a");
        var variable = GetVariable("a");
        var parameter = GetParameter("a");
        var function = GetFunction("a");
        var diagnostics = new Mock<IDiagnostics>();
        var manager = new LocalVariablesManager(diagnostics.Object);

        var finalManager = manager.Add(firstDeclaration).Add(variable).Add(parameter).Add(function);

        diagnostics.Verify(d => d.Report(It.IsAny<MultipleDeclarationsOfTheSameIdentifierError>()), Times.Exactly(3));
    }

    [Fact]
    public void VariableUsedAsFunctionReported()
    {
        var variable = GetVariable("a");
        var parameter = GetParameter("b");
        var diagnostics = new Mock<IDiagnostics>();
        var manager = new LocalVariablesManager(diagnostics.Object).Add(variable).Add(parameter);

        var variableAsFunction = manager.GetCalledFunctionDeclaration(new Identifier("a"));
        var parameterAsFunction = manager.GetCalledFunctionDeclaration(new Identifier("b"));

        diagnostics.Verify(d => d.Report(It.IsAny<NotAFunctionError>()), Times.Exactly(2));
        Assert.Null(variableAsFunction);
        Assert.Null(parameterAsFunction);
    }

    [Fact]
    public void FunctionUsedAsVariableReported()
    {
        var function = GetFunction("f");
        var diagnostics = new Mock<IDiagnostics>();
        var manager = new LocalVariablesManager(diagnostics.Object).Add(function);

        var functionAsVariable1 = manager.GetUsedVariableDeclaration(new Identifier("f"));
        var functionAsVariable2 = manager.GetAssignedVariableDeclaration(new Identifier("f"));

        diagnostics.Verify(d => d.Report(It.IsAny<NotAVariableError>()), Times.Exactly(2));
        Assert.Null(functionAsVariable1);
        Assert.Null(functionAsVariable2);
    }

    [Fact]
    public void NewScopeAllowsShadowing()
    {
        var variable1 = GetVariable("a");
        var parameter1 = GetParameter("b");
        var function1 = GetFunction("c");
        var variable2 = GetVariable("a");
        var parameter2 = GetParameter("b");
        var function2 = GetFunction("c");
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        var manager = new LocalVariablesManager(diagnostics.Object);

        var finalManager = manager
            .Add(variable1)
            .Add(parameter1)
            .Add(function1)
            .NewScope()
            .Add(variable2)
            .Add(parameter2)
            .Add(function2);

        Assert.Same(variable2, finalManager.GetAssignedVariableDeclaration(new Identifier("a")));
        Assert.Same(variable2, finalManager.GetUsedVariableDeclaration(new Identifier("a")));
        Assert.Same(parameter2, finalManager.GetUsedVariableDeclaration(new Identifier("b")));
        Assert.Same(function2, finalManager.GetCalledFunctionDeclaration(new Identifier("c")));
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
