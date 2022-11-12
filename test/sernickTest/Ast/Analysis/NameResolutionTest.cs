namespace sernickTest.AST.Analysis;

using System.Collections.Immutable;
using Moq;
using sernick.Ast;
using sernick.Ast.Analysis;
using sernick.Ast.Analysis.NameResolution;
using sernick.Ast.Nodes;
using sernick.Diagnostics;
using sernick.Input;
using sernick.Utility;
using Tokenizer.Lexer.Helpers;

public class NameResolutionTest
{
    private static readonly Range<ILocation> loc = new(new FakeInput.Location(0), new FakeInput.Location(0));

    // UsedVariable tests where variable is not a function argument
    [Fact]
    public void VariableUseFromTheSameScopeResolved()
    {
        // var x; x+1;
        var declaration = GetVariableDeclaration("x");
        var variableValue = GetVariableValue(GetIdentifier("x"));
        var infix = GetSimpleInfix(variableValue, GetIntLiteral(1), Infix.Op.Plus);
        var tree = GetExpressionJoin(declaration, infix);
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration, result.UsedVariableDeclarations[variableValue]);
    }

    [Fact]
    public void VariableUseAmongDifferentDeclarationsResolved()
    {
        // var y; var x; var z; x+1;
        var declaration1 = GetVariableDeclaration("y");
        var declaration2 = GetVariableDeclaration("x");
        var declaration3 = GetVariableDeclaration("z");
        var variableValue = GetVariableValue(GetIdentifier("x"));
        var infix = GetSimpleInfix(variableValue, GetIntLiteral(1), Infix.Op.Plus);
        var tree = GetExpressionJoin(GetExpressionJoin(declaration1, declaration2), GetExpressionJoin(declaration3, infix));
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration2, result.UsedVariableDeclarations[variableValue]);
    }

    [Fact]
    public void VariableUseFromOuterScopeResolved()
    {
        // var x; {x+1;}
        var declaration = GetVariableDeclaration("x");
        var variableValue = GetVariableValue(GetIdentifier("x"));
        var infix = GetSimpleInfix(variableValue, GetIntLiteral(1), Infix.Op.Plus);
        var tree = GetExpressionJoin(declaration, GetCodeBlock(infix));
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration, result.UsedVariableDeclarations[variableValue]);
    }

    [Fact]
    public void VariableUseFromOuterScopeShadowedResolved()
    {
        // var x; {var x; x+1;}
        var outerDeclaration = GetVariableDeclaration("x");
        var innerDeclaration = GetVariableDeclaration("x");
        var variableValue = GetVariableValue(GetIdentifier("x"));
        var infix = GetSimpleInfix(variableValue, GetIntLiteral(1), Infix.Op.Plus);
        var tree = GetExpressionJoin(outerDeclaration, GetCodeBlock(GetExpressionJoin(innerDeclaration, infix)));
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(innerDeclaration, result.UsedVariableDeclarations[variableValue]);
    }

    // AssignedVariable tests
    [Fact]
    public void VariableAssignmentFromTheSameScopeResolved()
    {
        // var x; x=1;
        var declaration = GetVariableDeclaration("x");
        var assignment = GetAssignment(GetIdentifier("x"), GetIntLiteral(1));
        var tree = GetExpressionJoin(declaration, assignment);
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration, result.AssignedVariableDeclarations[assignment]);
    }

    [Fact]
    public void VariableAssignmentAmongDifferentDeclarationsResolved()
    {
        // var y; var x; var z; x=1;
        var declaration1 = GetVariableDeclaration("y");
        var declaration2 = GetVariableDeclaration("x");
        var declaration3 = GetVariableDeclaration("z");
        var assignment = GetAssignment(GetIdentifier("x"), GetIntLiteral(1));
        var tree = GetExpressionJoin(GetExpressionJoin(declaration1, declaration2), GetExpressionJoin(declaration3, assignment));
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration2, result.AssignedVariableDeclarations[assignment]);
    }

    [Fact]
    public void VariableAssignmentFromOuterScopeResolved()
    {
        // var x; {x=1;}
        var declaration = GetVariableDeclaration("x");
        var assignment = GetAssignment(GetIdentifier("x"), GetIntLiteral(1));
        var tree = GetExpressionJoin(declaration, GetCodeBlock(assignment));
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration, result.AssignedVariableDeclarations[assignment]);
    }

    [Fact]
    public void VariableAssignmentFromOuterScopeShadowedResolved()
    {
        // var x; {var x; x=1;}
        var outerDeclaration = GetVariableDeclaration("x");
        var innerDeclaration = GetVariableDeclaration("x");
        var assignment = GetAssignment(GetIdentifier("x"), GetIntLiteral(1));
        var tree = GetExpressionJoin(outerDeclaration, GetCodeBlock(GetExpressionJoin(innerDeclaration, assignment)));
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(innerDeclaration, result.AssignedVariableDeclarations[assignment]);
    }

    // CalledFunction tests
    [Fact]
    public void CalledFunctionFromTheSameScopeResolved()
    {
        // fun f() : Int { return 0; }
        // f();
        var declaration = GetZeroArgumentFunctionDefinition("f");
        var call = GetFunctionCall(GetIdentifier("f"), Enumerable.Empty<Expression>());
        var tree = GetExpressionJoin(declaration, call);
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration, result.CalledFunctionDeclarations[call]);
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
        var call = GetFunctionCall(GetIdentifier("f"), Enumerable.Empty<Expression>());
        var tree = GetExpressionJoin(GetExpressionJoin(declaration1, declaration2), GetExpressionJoin(declaration3, call));
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration2, result.CalledFunctionDeclarations[call]);
    }

    [Fact]
    public void CalledFunctionFromOuterScopeResolved()
    {
        // fun f() : Int { return 0; }
        // { f(); }
        var declaration = GetZeroArgumentFunctionDefinition("f");
        var call = GetFunctionCall(GetIdentifier("f"), Enumerable.Empty<Expression>());
        var tree = GetExpressionJoin(declaration, GetCodeBlock(call));
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration, result.CalledFunctionDeclarations[call]);
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
        var call = GetFunctionCall(GetIdentifier("f"), Enumerable.Empty<Expression>());
        var tree = GetExpressionJoin(outerDeclaration, GetCodeBlock(GetExpressionJoin(innerDeclaration, call)));
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(innerDeclaration, result.CalledFunctionDeclarations[call]);
    }

    // UsedVariable tests where variable is a function argument
    [Fact]
    public void FunctionParameterUseResolved()
    {
        // fun f(const a : Int) : Int { return a; }
        var parameter = GetFunctionParameter("a");
        var use = GetVariableValue(GetIdentifier("a"));
        var block = GetCodeBlock(GetReturnStatement(use));
        var tree = GetOneArgumentFunctionDefinition("f", parameter, block);
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(parameter, result.UsedVariableDeclarations[use]);
    }

    [Fact]
    public void FunctionParameterShadowUseResolved()
    {
        // var a;
        // fun f(const a : Int) : Int { return a; }
        var parameter = GetFunctionParameter("a");
        var use = GetVariableValue(GetIdentifier("a"));
        var block = GetCodeBlock(GetReturnStatement(use));
        var function = GetOneArgumentFunctionDefinition("f", parameter, block);
        var tree = GetExpressionJoin(
            GetVariableDeclaration("a"),
            function);
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(parameter, result.UsedVariableDeclarations[use]);
    }

    [Fact]
    public void FunctionNameShadowedByParameterResolved()
    {
        //  fun f(const f : Int) : Int { return f; }
        var parameter = GetFunctionParameter("f");
        var use = GetVariableValue(GetIdentifier("f"));
        var tree = GetOneArgumentFunctionDefinition("f", parameter, GetCodeBlock(GetReturnStatement(use)));
        var diagnostics = new Mock<IDiagnostics>();

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(parameter, result.UsedVariableDeclarations[use]);
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
        var use = GetVariableValue(GetIdentifier("a"));
        var blockG = GetCodeBlock(GetReturnStatement(use));
        var functionG = GetOneArgumentFunctionDefinition("g", parameterB, blockG);
        var parameterA = GetFunctionParameter("a");
        var tree = GetOneArgumentFunctionDefinition("f", parameterA, GetCodeBlock(functionG));
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(parameterA, result.UsedVariableDeclarations[use]);
    }

    // UndeclaredIdentifier tests
    [Fact]
    public void UndefinedIdentifierInVariableUseReported()
    {
        // a;
        var tree = GetVariableValue(GetIdentifier("a"));
        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<UndeclaredIdentifierError>()));
    }

    [Fact]
    public void UndefinedIdentifierInVariableAssignmentReported()
    {
        // a = 3;
        var tree = GetAssignment(GetIdentifier("a"), GetIntLiteral(3));
        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<UndeclaredIdentifierError>()));
    }

    [Fact]
    public void UndefinedIdentifierInFunctionCallReported()
    {
        // f();
        var tree = GetFunctionCall(GetIdentifier("f"), Enumerable.Empty<Expression>());
        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<UndeclaredIdentifierError>()));
    }

    // MultipleDeclaration tests
    [Fact]
    public void MultipleDeclarationsReported_Case1()
    {
        // var a;
        // var a;
        var declaration1 = GetVariableDeclaration("a");
        var declaration2 = GetVariableDeclaration("a");
        var tree = GetExpressionJoin(declaration1, declaration2);
        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<MultipleDeclarationsError>()));
    }

    [Fact]
    public void MultipleDeclarationsReported_Case2()
    {
        // fun f(const a : Int) : Int
        // {
        //   var a;
        //   return 0;
        // }
        var parameter = GetFunctionParameter("a");
        var block = GetCodeBlock(GetExpressionJoin(GetVariableDeclaration("a"), GetReturnStatement(GetIntLiteral(0))));
        var tree = GetOneArgumentFunctionDefinition("f", parameter, block);
        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<MultipleDeclarationsError>()));
    }

    [Fact]
    public void MultipleDeclarationsReported_Case3()
    {
        //  fun f() : Int { return 0; }
        //  var f;
        var function = GetZeroArgumentFunctionDefinition("f");
        var declaration = GetVariableDeclaration("f");
        var tree = GetExpressionJoin(function, declaration);
        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<MultipleDeclarationsError>()));
    }

    // NotAFunction tests
    [Fact]
    public void NotAFunctionReported_Case1()
    {
        //  var f;
        //  f();
        var declaration = GetVariableDeclaration("f");
        var call = GetFunctionCall(GetIdentifier("f"), Enumerable.Empty<Expression>());
        var tree = GetExpressionJoin(declaration, call);
        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<NotAFunctionError>()));
    }

    [Fact]
    public void NotAFunctionReported_Case2()
    {
        //  fun f(var a : Int) : Int
        //  {
        //      return a();
        //  }
        var body = GetCodeBlock(GetReturnStatement(GetFunctionCall(GetIdentifier("a"), Enumerable.Empty<Expression>())));
        var parameter = GetFunctionParameter("a");
        var tree = GetOneArgumentFunctionDefinition("f", parameter, body);
        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<NotAFunctionError>()));
    }

    // NotAVariable tests
    [Fact]
    public void NotAVariableReported_Case1()
    {
        // fun f() : Int { return 0; }
        // f+1;
        var function = GetZeroArgumentFunctionDefinition("f");
        var tree = GetExpressionJoin(function,
            GetSimpleInfix(GetVariableValue(GetIdentifier("f")), GetIntLiteral(1), Infix.Op.Plus));
        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<NotAVariableError>()));
    }

    [Fact]
    public void NotAVariableReported_Case2()
    {
        // fun f() : Int { return 0; }
        // f=1;
        var function = GetZeroArgumentFunctionDefinition("f");
        var tree = GetExpressionJoin(function, GetAssignment(GetIdentifier("f"), GetIntLiteral(1)));
        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<NotAVariableError>()));
    }

    private static VariableDeclaration GetVariableDeclaration(string name)
    {
        return new VariableDeclaration(GetIdentifier(name), null, null, false, loc);
    }

    private static FunctionDefinition GetZeroArgumentFunctionDefinition(string name)
    {
        return new FunctionDefinition(GetIdentifier(name),
            ImmutableArray<FunctionParameterDeclaration>.Empty,
            new IntType(),
            GetCodeBlock(GetReturnStatement(GetIntLiteral(0))),
            loc);
    }

    private static FunctionParameterDeclaration GetFunctionParameter(string name)
    {
        return new FunctionParameterDeclaration(GetIdentifier(name), new IntType(), null, loc);
    }

    private static FunctionDefinition GetOneArgumentFunctionDefinition(string functionName, FunctionParameterDeclaration parameter, CodeBlock block)
    {
        return new FunctionDefinition(GetIdentifier(functionName),
            new[] { parameter },
            new IntType(),
            block,
            loc);
    }

    private static Identifier GetIdentifier(string name)
    {
        return new Identifier(name, loc);
    }

    private static LiteralValue GetIntLiteral(int n)
    {
        return new IntLiteralValue(n, loc);
    }

    private static VariableValue GetVariableValue(Identifier identifier)
    {
        return new VariableValue(identifier, loc);
    }

    private static Infix GetSimpleInfix(Expression e1, Expression e2, Infix.Op op)
    {
        return new Infix(e1, e2, op, loc);
    }

    private static ExpressionJoin GetExpressionJoin(Expression e1, Expression e2)
    {
        return new ExpressionJoin(e1, e2, loc);
    }

    private static CodeBlock GetCodeBlock(Expression e)
    {
        return new CodeBlock(e, loc);
    }

    private static Assignment GetAssignment(Identifier identifier, Expression e)
    {
        return new Assignment(identifier, e, loc);
    }

    private static FunctionCall GetFunctionCall(Identifier identifier, IEnumerable<Expression> args)
    {
        return new FunctionCall(identifier, args, loc);
    }

    private static ReturnStatement GetReturnStatement(Expression e)
    {
        return new ReturnStatement(e, loc);
    }
}
