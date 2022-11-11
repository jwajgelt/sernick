namespace sernickTest.AST.Analysis;

using System.Collections.Immutable;
using Moq;
using sernick.Ast;
using sernick.Ast.Analysis.NameResolution;
using sernick.Ast.Analysis.NameResolution.Errors;
using sernick.Ast.Nodes;
using sernick.Diagnostics;

public class NameResolutionTest
{
    // UsedVariable tests where variable is not a function argument
    [Fact]
    public void VariableUseFromTheSameScopeResolved()
    {
        // var x; x+1;
        var declaration = new VariableDeclaration(new Identifier("x"), null, null, true);
        var variableValue = new VariableValue(new Identifier("x"));
        var infix = new Infix(variableValue, new IntLiteralValue(1), Infix.Op.Equals);
        var tree = new ExpressionJoin(declaration, infix);
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        
        var nameResolution = new Algorithm(tree, diagnostics.Object);
        
        Assert.Same(declaration, nameResolution.UsedVariableDeclarations[variableValue]);
    }
    
    [Fact]
    public void VariableUseAmongDifferentDeclarationsResolved()
    {
        // var y; var x; var z; x+1;
        var declaration1 = new VariableDeclaration(new Identifier("y"), null, null, true);
        var declaration2 = new VariableDeclaration(new Identifier("x"), null, null, true);
        var declaration3 = new VariableDeclaration(new Identifier("z"), null, null, true);
        var variableValue = new VariableValue(new Identifier("x"));
        var infix = new Infix(variableValue, new IntLiteralValue(1), Infix.Op.Equals);
        var tree = new ExpressionJoin(new ExpressionJoin(declaration1, declaration2), new ExpressionJoin(declaration3, infix));
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        
        var nameResolution = new Algorithm(tree, diagnostics.Object);
        
        Assert.Same(declaration2, nameResolution.UsedVariableDeclarations[variableValue]);
    }
    
    [Fact]
    public void VariableUseFromOuterScopeResolved()
    {
        // var x; {x+1;}
        var declaration = new VariableDeclaration(new Identifier("x"), null, null, true);
        var variableValue = new VariableValue(new Identifier("x"));
        var infix = new Infix(variableValue, new IntLiteralValue(1), Infix.Op.Equals);
        var tree = new ExpressionJoin(declaration, new CodeBlock(infix));
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        
        var nameResolution = new Algorithm(tree, diagnostics.Object);
        
        Assert.Same(declaration, nameResolution.UsedVariableDeclarations[variableValue]);
    }
    
    [Fact]
    public void VariableUseFromOuterScopeShadowedResolved()
    {
        // var x; {var x; x+1;}
        var outerDeclaration = new VariableDeclaration(new Identifier("x"), null, null, true);
        var innerDeclaration = new VariableDeclaration(new Identifier("x"), null, null, true);
        var variableValue = new VariableValue(new Identifier("x"));
        var infix = new Infix(variableValue, new IntLiteralValue(1), Infix.Op.Equals);
        var tree = new ExpressionJoin(outerDeclaration, new CodeBlock(new ExpressionJoin(innerDeclaration, infix)));
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        
        var nameResolution = new Algorithm(tree, diagnostics.Object);
        
        Assert.Same(innerDeclaration, nameResolution.UsedVariableDeclarations[variableValue]);
    }
    
    // AssignedVariable tests
    [Fact]
    public void VariableAssignmentFromTheSameScopeResolved()
    {
        // var x; x=1;
        var declaration = new VariableDeclaration(new Identifier("x"), null, null, true);
        var assignment = new Assignment(new Identifier("x"), new IntLiteralValue(1));
        var tree = new ExpressionJoin(declaration, assignment);
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        
        var nameResolution = new Algorithm(tree, diagnostics.Object);
        
        Assert.Same(declaration, nameResolution.AssignedVariableDeclarations[assignment]);
    }
    
    [Fact]
    public void VariableAssignmentAmongDifferentDeclarationsResolved()
    {
        // var y; var x; var z; x=1;
        var declaration1 = new VariableDeclaration(new Identifier("y"), null, null, true);
        var declaration2 = new VariableDeclaration(new Identifier("x"), null, null, true);
        var declaration3 = new VariableDeclaration(new Identifier("z"), null, null, true);
        var assignment = new Assignment(new Identifier("x"), new IntLiteralValue(1));
        var tree = new ExpressionJoin(new ExpressionJoin(declaration1, declaration2), new ExpressionJoin(declaration3, assignment));
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        
        var nameResolution = new Algorithm(tree, diagnostics.Object);
        
        Assert.Same(declaration2, nameResolution.AssignedVariableDeclarations[assignment]);
    }
    
    [Fact]
    public void VariableAssignmentFromOuterScopeResolved()
    {
        // var x; {x=1;}
        var declaration = new VariableDeclaration(new Identifier("x"), null, null, true);
        var assignment = new Assignment(new Identifier("x"), new IntLiteralValue(1));
        var tree = new ExpressionJoin(declaration, new CodeBlock(assignment));
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        
        var nameResolution = new Algorithm(tree, diagnostics.Object);
        
        Assert.Same(declaration, nameResolution.AssignedVariableDeclarations[assignment]);
    }
    
    [Fact]
    public void VariableAssignmentFromOuterScopeShadowedResolved()
    {
        // var x; {var x; x=1;}
        var outerDeclaration = new VariableDeclaration(new Identifier("x"), null, null, true);
        var innerDeclaration = new VariableDeclaration(new Identifier("x"), null, null, true);
        var assignment = new Assignment(new Identifier("x"), new IntLiteralValue(1));
        var tree = new ExpressionJoin(outerDeclaration, new CodeBlock(new ExpressionJoin(innerDeclaration, assignment)));
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        
        var nameResolution = new Algorithm(tree, diagnostics.Object);
        
        Assert.Same(innerDeclaration, nameResolution.AssignedVariableDeclarations[assignment]);
    }
    
    // CalledFunction tests
    [Fact]
    public void CalledFunctionFromTheSameScopeResolved()
    {
        // fun f() : Int { return 0; }
        // f();
        var declaration = GetZeroArgumentFunctionDefinition("f");
        var call = new FunctionCall(new Identifier("f"), Enumerable.Empty<Expression>());
        var tree = new ExpressionJoin(declaration, call);
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        
        var nameResolution = new Algorithm(tree, diagnostics.Object);
        
        Assert.Same(declaration, nameResolution.CalledFunctionDeclarations[call]);
    }
    
    [Fact]
    public void CalledFunctionAmongDifferentDeclarationsResolved()
    {
        // fun g() : Int { return 0; }
        // fun f() : Int { return 0; }
        // fun h() : Int { return 0; }
        // f();
        var declaration1 = GetZeroArgumentFunctionDefinition("g");
        var declaration2 = GetZeroArgumentFunctionDefinition("f");
        var declaration3 = GetZeroArgumentFunctionDefinition("h");
        var call = new FunctionCall(new Identifier("f"), Enumerable.Empty<Expression>());
        var tree = new ExpressionJoin(new ExpressionJoin(declaration1, declaration2), new ExpressionJoin(declaration3, call));
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        
        var nameResolution = new Algorithm(tree, diagnostics.Object);

        Assert.Same(declaration2, nameResolution.CalledFunctionDeclarations[call]);
    }
    
    [Fact]
    public void CalledFunctionFromOuterScopeResolved()
    {
        // fun f() : Int { return 0; }
        // { f(); }
        var declaration = GetZeroArgumentFunctionDefinition("f");
        var call = new FunctionCall(new Identifier("f"), Enumerable.Empty<Expression>());
        var tree = new ExpressionJoin(declaration, new CodeBlock(call));
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        
        var nameResolution = new Algorithm(tree, diagnostics.Object);
        
        Assert.Same(declaration, nameResolution.CalledFunctionDeclarations[call]);
    }
    
    [Fact]
    public void CalledFunctionFromOuterScopeShadowedResolved()
    {
        // fun f() : Int { return 0; }
        // {
        //   fun f() : Int { return 0; }
        //   f();
        // }
        var outerDeclaration = GetZeroArgumentFunctionDefinition("f");
        var innerDeclaration = GetZeroArgumentFunctionDefinition("f");
        var call = new FunctionCall(new Identifier("f"), Enumerable.Empty<Expression>());
        var tree = new ExpressionJoin(outerDeclaration, new CodeBlock(new ExpressionJoin(innerDeclaration, call)));
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        
        var nameResolution = new Algorithm(tree, diagnostics.Object);
        
        Assert.Same(innerDeclaration, nameResolution.CalledFunctionDeclarations[call]);
    }
    
    // UsedVariable tests where variable is not a function argument
    [Fact]
    public void FunctionParameterUseResolved()
    {
        // fun f(const a : Int) : Int { return a; }
        var parameter = GetFunctionParameter("a");
        var use = new VariableValue(new Identifier("a"));
        var block = new CodeBlock(new ReturnStatement(use));
        var function = GetOneArgumentFunctionDefinition("f", parameter, block);
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        
        var nameResolution = new Algorithm(function, diagnostics.Object);
        
        Assert.Same(parameter, nameResolution.UsedVariableDeclarations[use]);
    }
    
    [Fact]
    public void FunctionParameterShadowUseResolved()
    {
        // var a;
        // fun f(const a : Int) : Int { return a; }
        var parameter = GetFunctionParameter("a");
        var use = new VariableValue(new Identifier("a"));
        var block = new CodeBlock(new ReturnStatement(use));
        var function = GetOneArgumentFunctionDefinition("f", parameter, block);
        var tree = new ExpressionJoin(
            new VariableDeclaration(new Identifier("a"), null, null, false),
            function);
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        
        var nameResolution = new Algorithm(tree, diagnostics.Object);
        
        Assert.Same(parameter, nameResolution.UsedVariableDeclarations[use]);
    }
    
    [Fact]
    public void FunctionParameterFromOuterScopeResolved()
    {
        // fun f(const a : Int) : Int
        // {
        //   fun g(const b : Int) : Int
        //   {
        //     return a;
        //   }
        // }
        var parameterB = GetFunctionParameter("b");
        var use = new VariableValue(new Identifier("a"));
        var blockG = new CodeBlock(new ReturnStatement(use));
        var functionG = GetOneArgumentFunctionDefinition("g", parameterB, blockG);
        var parameterA = GetFunctionParameter("a");
        var functionF = GetOneArgumentFunctionDefinition("f", parameterA, new CodeBlock(functionG));
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        
        var nameResolution = new Algorithm(functionF, diagnostics.Object);
        
        Assert.Same(parameterA, nameResolution.UsedVariableDeclarations[use]);
    }
    
    // UndeclaredIdentifier tests
    [Fact]
    private void UndefinedIdentifierInVariableUseReported()
    {
        // a;
        var use = new VariableValue(new Identifier("a"));
        var diagnostics = new Mock<IDiagnostics>();
        
        var nameResolution = new Algorithm(use, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<UndeclaredIdentifierError>()));
    }
    
    [Fact]
    private void UndefinedIdentifierInVariableAssignmentReported()
    {
        // a = 3;
        var assignment = new Assignment(new Identifier("a"), new IntLiteralValue(3));
        var diagnostics = new Mock<IDiagnostics>();
        
        var nameResolution = new Algorithm(assignment, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<UndeclaredIdentifierError>()));
    }
    
    [Fact]
    private void UndefinedIdentifierInFunctionCallReported()
    {
        // f();
        var call = new FunctionCall(new Identifier("f"), Enumerable.Empty<Expression>());
        var diagnostics = new Mock<IDiagnostics>();
        
        var nameResolution = new Algorithm(call, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<UndeclaredIdentifierError>()));
    }
    
    // MultipleDeclaration tests
    [Fact]
    private void MultipleDeclarationsResolved_Case1()
    {
        // var a;
        // var a;
        var declaration1 = new VariableDeclaration(new Identifier("a"), null, null, false);
        var declaration2 = new VariableDeclaration(new Identifier("a"), null, null, false);
        var tree = new ExpressionJoin(declaration1, declaration2);
        var diagnostics = new Mock<IDiagnostics>();

        var nameResolution = new Algorithm(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<MultipleDeclarationsOfTheSameIdentifierError>()));
    }
    
    [Fact]
    private void MultipleDeclarationsResolved_Case2()
    {
        // fun f(const a : Int) : Int
        // {
        //   var a;
        //   return 0;
        // }
        var parameter = GetFunctionParameter("a");
        var block = new CodeBlock(new ExpressionJoin(GetVariableDeclaration("a"), new ReturnStatement(new IntLiteralValue(0))));
        var function = GetOneArgumentFunctionDefinition("f", parameter, block);
        var diagnostics = new Mock<IDiagnostics>();

        var nameResolution = new Algorithm(function, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<MultipleDeclarationsOfTheSameIdentifierError>()));
    }
    
    [Fact]
    private void MultipleDeclarationsResolved_Case3()
    {
        //  fun f() : Int { return 0; }
        //  var f;
        var function = GetZeroArgumentFunctionDefinition("f");
        var declaration = new VariableDeclaration(new Identifier("f"), null, null, false);
        var tree = new ExpressionJoin(function, declaration);
        var diagnostics = new Mock<IDiagnostics>();

        var nameResolution = new Algorithm(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<MultipleDeclarationsOfTheSameIdentifierError>()));
    }

    private static VariableDeclaration GetVariableDeclaration(string name)
    {
        return new VariableDeclaration(new Identifier(name), null, null, false);
    }
    
    private static FunctionDefinition GetZeroArgumentFunctionDefinition(string name)
    {
        return new FunctionDefinition(new Identifier(name), 
            ImmutableArray<FunctionParameterDeclaration>.Empty,
            new IntType(),
            new CodeBlock(new ReturnStatement(new IntLiteralValue(0))));
    }

    private static FunctionParameterDeclaration GetFunctionParameter(string name)
    {
        return new FunctionParameterDeclaration(new Identifier(name), new IntType(), null);
    }
    
    private static FunctionDefinition GetOneArgumentFunctionDefinition(string functionName, FunctionParameterDeclaration parameter, CodeBlock block)
    {
        return new FunctionDefinition(new Identifier(functionName),
            new[] { parameter },
            new IntType(),
            block);
    }
}
