namespace sernickTest.Ast.Analysis;

using Input;
using Moq;
using sernick.Ast;
using sernick.Ast.Analysis;
using sernick.Ast.Analysis.NameResolution;
using sernick.Diagnostics;
using sernick.Input;
using sernick.Utility;
using static Helpers.AstNodesExtensions;

public class NameResolutionTest
{
    private static readonly Range<ILocation> loc = new(new FakeLocation(), new FakeLocation());

    // UsedVariable tests where variable is not a function argument
    [Fact]
    public void VariableUseFromTheSameScopeResolved()
    {
        // var x; x + 1
        var tree = Program(
            Var("x", out var declaration),
            Value("x", out var variableValue).Plus(1)
        );
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration, result.UsedVariableDeclarations[variableValue]);
    }

    [Fact]
    public void VariableUseAmongDifferentDeclarationsResolved()
    {
        // var y; var x; var z; x+1;
        var tree = Program(
            Var("y"),
            Var("x", out var declarationX),
            Var("z"),
            Value("x", out var variableValue).Plus(1)
        );
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declarationX, result.UsedVariableDeclarations[variableValue]);
    }

    [Fact]
    public void VariableUseFromOuterScopeResolved()
    {
        // var x; {x+1;}
        var tree = Program(
            Var("x", out var declaration),
            Block(
                Value("x", out var variableValue).Plus(1)
            )
        );
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration, result.UsedVariableDeclarations[variableValue]);
    }

    [Fact]
    public void VariableUseFromOuterScopeShadowedResolved()
    {
        // var x; {var x; x+1;}
        var tree = Program(
            Var("x"),
            Block(
                Var("x", out var innerDeclaration),
                Value("x", out var variableValue).Plus(1)
            )
        );
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(innerDeclaration, result.UsedVariableDeclarations[variableValue]);
    }

    // AssignedVariable tests
    [Fact]
    public void VariableAssignmentFromTheSameScopeResolved()
    {
        // var x; x=1;
        var tree = Program(
            Var("x", out var declaration),
            "x".Assign(1, out var assignment)
        );
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration, result.AssignedVariableDeclarations[assignment]);
    }

    [Fact]
    public void VariableAssignmentAmongDifferentDeclarationsResolved()
    {
        // var y; var x; var z; x=1;
        var tree = Program(
            Var("y"),
            Var("x", out var declarationX),
            Var("z"),
            "x".Assign(1, out var assignment)
        );
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declarationX, result.AssignedVariableDeclarations[assignment]);
    }

    [Fact]
    public void VariableAssignmentFromOuterScopeResolved()
    {
        // var x; {x=1;}
        var tree = Program(
            Var("x", out var declaration),
            Block(
                "x".Assign(1, out var assignment)
            )
        );
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration, result.AssignedVariableDeclarations[assignment]);
    }

    [Fact]
    public void VariableAssignmentFromOuterScopeShadowedResolved()
    {
        // var x; {var x; x=1;}
        var tree = Program(
            Var("x"),
            Block(
                Var("x", out var innerDeclaration),
                "x".Assign(1, out var assignment)
            )
        );
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
        var tree = Program(
            Fun<IntType>("f").Body(
                Return(0), Close
            ).Get(out var declaration),
            "f".Call(out var call)
        );
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
        var tree = Program(
            Fun<IntType>("g").Body(
                Return(0), Close
            ).Get(),
            Fun<IntType>("f").Body(
                Return(0), Close
            ).Get(out var declarationF),
            Fun<IntType>("h").Body(
                Return(0), Close
            ).Get(),
            "f".Call(out var call)
        );
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declarationF, result.CalledFunctionDeclarations[call]);
    }

    [Fact]
    public void CalledFunctionFromOuterScopeResolved()
    {
        // fun f() : Int { return 0; }
        // { f(); }
        var tree = Program(
            Fun<IntType>("f").Body(
                Return(0), Close
            ).Get(out var declaration),
            Block(
                "f".Call(out var call)
            )
        );
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
        var tree = Program(
            Fun<IntType>("f").Body(
                Return(0), Close
            ).Get(),
            Block(
                Fun<IntType>("f").Body(
                    Return(0), Close
                ).Get(out var innerDeclaration),
                "f".Call(out var call)
            )
        );
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(innerDeclaration, result.CalledFunctionDeclarations[call]);
    }

    // UsedVariable tests where variable is a function argument
    [Fact]
    public void FunctionParameterUseResolved()
    {
        // fun f(const a : Int) : Int { return a; }
        var tree = Fun<IntType>("f").Parameter<IntType>("a", out var parameter).Body(
            Return(Value("a", out var use)), Close
        ).Get();
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(parameter, result.UsedVariableDeclarations[use]);
    }

    [Fact]
    public void FunctionParameterShadowUseResolved()
    {
        // var a;
        // fun f(const a : Int) : Int { return a; }
        var tree = Program(
            Var("a"),
            Fun<IntType>("f").Parameter<IntType>("a", out var parameter).Body(
                Return(Value("a", out var use)), Close
            ).Get()
        );
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(parameter, result.UsedVariableDeclarations[use]);
    }

    [Fact]
    public void FunctionNameShadowedByParameterResolved()
    {
        //  fun f(const f : Int) : Int { return f; }
        var tree = Fun<IntType>("f").Parameter<IntType>("f", out var parameter).Body(
            Return(Value("f", out var use)), Close
        ).Get();
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
        var tree = Fun<IntType>("f").Parameter<IntType>("a", out var parameterA).Body(
            Fun<IntType>("g").Parameter<IntType>("b").Body(
                Return(Value("a", out var use)), Close
            ).Get()
        ).Get();
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(parameterA, result.UsedVariableDeclarations[use]);
    }

    // UndeclaredIdentifier tests
    [Fact]
    public void UndefinedIdentifierInVariableUseReported()
    {
        // a;
        var tree = Value("a");
        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<UndeclaredIdentifierError>()));
    }

    [Fact]
    public void UndefinedIdentifierInVariableAssignmentReported()
    {
        // a = 3;
        var tree = "a".Assign(3);
        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<UndeclaredIdentifierError>()));
    }

    [Fact]
    public void UndefinedIdentifierInFunctionCallReported()
    {
        // f();
        var tree = "f".Call();
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
        var tree = Program(
            Var("a"),
            Var("a")
        );
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
        var tree = Fun<IntType>("f").Parameter<IntType>("a").Body(
            Var("a"),
            Return(0), Close
        ).Get();
        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<MultipleDeclarationsError>()));
    }

    [Fact]
    public void MultipleDeclarationsReported_Case3()
    {
        //  fun f() : Int { return 0; }
        //  var f;
        var tree = Program(
            Fun<IntType>("f").Body(
                Return(0), Close
            ).Get(),
            Var("f")
        );
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
        var tree = Program(Var("f"), "f".Call());
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
        var tree = Fun<IntType>("f").Parameter<IntType>("a").Body(
            Return("a".Call()), Close
        ).Get();
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
        var tree = Program(
            Fun<IntType>("f").Body(
                Return(0), Close
            ).Get(),
            "f".Plus(1)
        );
        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<NotAVariableError>()));
    }

    [Fact]
    public void NotAVariableReported_Case2()
    {
        // fun f() : Int { return 0; }
        // f=1;
        var tree = Program(
            Fun<IntType>("f").Body(
                Return(0), Close
            ).Get(),
            "f".Assign(1)
        );
        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<NotAVariableError>()));
    }
}
