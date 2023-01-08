namespace sernickTest.Ast.Analysis;

using Moq;
using sernick.Ast;
using sernick.Ast.Analysis;
using sernick.Ast.Analysis.NameResolution;
using sernick.Diagnostics;
using static Helpers.AstNodesExtensions;

public class NameResolutionTest
{
    // UsedVariable tests where variable is not a function argument
    [Fact]
    public void VariableUseFromTheSameScopeResolved()
    {
        // var x; x+1; x+1
        var tree = Program(
            Var("x", out var declaration),
            Value("x", out var variableValue1).Plus(1),
            Value("x", out var variableValue2).Plus(1)
        );
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration, result.UsedVariableDeclarations[variableValue1]);
        Assert.Same(declaration, result.UsedVariableDeclarations[variableValue2]);
    }

    [Fact]
    public void VariableUseAmongDifferentDeclarationsResolved()
    {
        // var y; var x; var z; x+1
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
        // var x; {x+1}
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
        // var x; {var x; x+1}
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
        // var x; x=1; x=1
        var tree = Program(
            Var("x", out var declaration),
            "x".Assign(1, out var assignment1),
            "x".Assign(1, out var assignment2)
        );
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration, result.AssignedVariableDeclarations[assignment1]);
        Assert.Same(declaration, result.AssignedVariableDeclarations[assignment2]);
    }

    [Fact]
    public void VariableAssignmentAmongDifferentDeclarationsResolved()
    {
        // var y; var x; var z; x=1
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
        // var x; {x=1}
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
        // var x; {var x; x=1}
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
        // f()
        var tree = Program(
            Fun<IntType>("f").Body(
                Return(0), Close
            ).Get(out var declaration),
            "f".Call(out var call1),
            "f".Call(out var call2)
        );
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration, result.CalledFunctionDeclarations[call1]);
        Assert.Same(declaration, result.CalledFunctionDeclarations[call2]);
    }

    [Fact]
    public void CalledFunctionAmongDifferentDeclarationsResolved()
    {
        // fun g() : Int { return 0; }
        // fun f() : Int { return 0; }
        // fun h() : Int { return 0; }
        // f()
        var tree = Program(
            Fun<IntType>("g").Body(
                Return(0), Close
            ),
            Fun<IntType>("f").Body(
                Return(0), Close
            ).Get(out var declarationF),
            Fun<IntType>("h").Body(
                Return(0), Close
            ),
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
        // { f() }
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
        //   f()
        // }
        var tree = Program(
            Fun<IntType>("f").Body(
                Return(0), Close
            ),
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
        // fun f(a : Int) : Int { return a; }
        var tree = Fun<IntType>("f").Parameter<IntType>("a", out var parameter).Body(
            Return(Value("a", out var use)), Close
        );
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(parameter, result.UsedVariableDeclarations[use]);
    }

    [Fact]
    public void FunctionParameterShadowUseResolved()
    {
        // var a;
        // fun f(a : Int) : Int { return a; }
        var tree = Program(
            Var("a"),
            Fun<IntType>("f").Parameter<IntType>("a", out var parameter).Body(
                Return(Value("a", out var use)), Close
            )
        );
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(parameter, result.UsedVariableDeclarations[use]);
    }

    [Fact]
    public void FunctionNameShadowedByParameterResolved()
    {
        //  fun f(f : Int) : Int { return f; }
        var tree = Fun<IntType>("f").Parameter<IntType>("f", out var parameter).Body(
            Return(Value("f", out var use)), Close
        );
        var diagnostics = new Mock<IDiagnostics>();

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(parameter, result.UsedVariableDeclarations[use]);
    }

    [Fact]
    public void FunctionParameterFromOuterScopeResolved()
    {
        // fun f(a : Int) : Int
        // {
        //   fun g(b : Int) : Int
        //   {
        //     return a;
        //   }
        // }
        var tree = Fun<IntType>("f").Parameter<IntType>("a", out var parameterA).Body(
            Fun<IntType>("g").Parameter<IntType>("b").Body(
                Return(Value("a", out var use)), Close
            )
        );
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(parameterA, result.UsedVariableDeclarations[use]);
    }

    // UndeclaredIdentifier tests
    [Fact]
    public void UndefinedIdentifierInVariableUseReported()
    {
        // a
        var tree = Value("a");
        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<UndeclaredIdentifierError>()));
    }

    [Fact]
    public void UndefinedIdentifierInVariableAssignmentReported()
    {
        // a = 3
        var tree = "a".Assign(3);
        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<UndeclaredIdentifierError>()));
    }

    [Fact]
    public void UndefinedIdentifierInFunctionCallReported()
    {
        // f()
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
        // var a
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
        // fun f(a : Int) : Int
        // {
        //   var a;
        //   return 0;
        // }
        var tree = Fun<IntType>("f").Parameter<IntType>("a").Body(
            Var("a"),
            Return(0), Close
        );
        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<MultipleDeclarationsError>()));
    }

    [Fact]
    public void MultipleDeclarationsReported_Case3()
    {
        //  fun f() : Int { return 0; }
        //  var f
        var tree = Program(
            Fun<IntType>("f").Body(
                Return(0), Close
            ),
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
        //  f()
        var tree = Program(Var("f"), "f".Call());
        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<NotAFunctionError>()));
    }

    [Fact]
    public void NotAFunctionReported_Case2()
    {
        //  fun f(a : Int) : Int
        //  {
        //      return a();
        //  }
        var tree = Fun<IntType>("f").Parameter<IntType>("a").Body(
            Return("a".Call()), Close
        );
        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<NotAFunctionError>()));
    }

    // NotAVariable tests
    [Fact]
    public void NotAVariableReported_Case1()
    {
        // fun f() : Int { return 0; }
        // f+1
        var tree = Program(
            Fun<IntType>("f").Body(
                Return(0), Close
            ),
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
        // f=1
        var tree = Program(
            Fun<IntType>("f").Body(
                Return(0), Close
            ),
            "f".Assign(1)
        );
        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<NotAVariableError>()));
    }

    [Fact]
    public void ArgumentResolved()
    {
        // fun f(a: Int): Int { return 0; }
        // var x = 0;
        // f(x);
        var tree = Program(
            Fun<IntType>("f").Parameter<IntType>("a").Body(Return(0)),
            Var("x", 1, out var declaration),
            "f".Call().Argument(Value("x", out var argument))
        );
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration, result.UsedVariableDeclarations[argument]);
    }

    [Fact]
    public void ShadowFunctionWithVariableCollision()
    {
        // fun f() : Int { return 0; }
        // var f;
        var tree = Program(
            Fun<IntType>("f").Body(Return(0)),
            Var("f")
        );
        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<MultipleDeclarationsError>()));
    }

    [Fact]
    public void DeclarationInAssignmentWithCollision()
    {
        // var y : Int = (var y: Int = 1; y);
        var tree = Program(
            Var<IntType>("y", Group(
                Var("y", 1),
                Value("y"))
            )
        );
        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<MultipleDeclarationsError>()));
    }

    [Fact]
    public void DeclarationInCallWithCollision()
    {
        // fun f(a: Int): Int { return 0; }
        // f((const f: Int = 1; f));
        var tree = Program(
            Fun<IntType>("f").Parameter<IntType>("f").Body(Return(0)),
            "f".Call().Argument(
                Group(
                    Var("f", 1),
                    Value("f")
                )
            )
        );
        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<MultipleDeclarationsError>()));
    }

    [Fact]
    public void DeclarationInAssignmentResolved()
    {
        // var y : Int = (const x: Int = 1; x);
        // x
        var tree = Program(
            Var<IntType>("y", Group(
                    Var("x", 1, out var declaration),
                    Value("x"))),
            Value("x", out var value)
        );
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration, result.UsedVariableDeclarations[value]);
    }

    [Fact]
    public void DeclarationInArgumentResolved_Case2()
    {
        // fun f(a: Int): Int { return 0; }
        //
        // f((const x: Int = 1; x));
        // x;
        var tree = Program(
            Fun<IntType>("f").Parameter<IntType>("a").Body(Return(0)),
            "f".Call().Argument(
                Group(
                    Var("x", 1, out var declaration),
                    Value("x")
                )
            ),
            Value("x", out var value)
        );
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration, result.UsedVariableDeclarations[value]);
    }

    [Fact]
    public void DeclarationInArgumentResolved_Case1()
    {
        // fun f(a: Int, b: Int): Int { return 0; }
        //
        // f((const x: Int = 1; x), x);
        var tree = Program(
            Fun<IntType>("f").Parameter<IntType>("a").Parameter<IntType>("b").Body(Return(0)),
            "f".Call().Argument(
                Group(
                    Var("x", 1, out var declaration),
                    Value("x", out var value1)
                )
            ).Argument(Value("x", out var value2))
        );
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration, result.UsedVariableDeclarations[value1]);
        Assert.Same(declaration, result.UsedVariableDeclarations[value2]);
    }

    [Fact]
    public void VariableNotUsableInOwnDeclaration()
    {
        // var x : Int = x
        var tree = Program(Var<IntType>("x", Value("x")));
        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<UndeclaredIdentifierError>()));
    }

    [Fact]
    public void ShadowedVariableStillUsableInDeclaration()
    {
        // var x : Int = 1;
        // {
        //   var x : Int = x;
        // }
        var tree = Program(
            Var<IntType>("x", Value("x"), out var declaration),
            Block(
                Var<IntType>("x", Value("x", out var value))
                )
            );
        var diagnostics = new Mock<IDiagnostics>();

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration, result.UsedVariableDeclarations[value]);
    }

    [Fact]
    public void CollisionInParametersReported()
    {
        //  fun f(a : Int, a : Int) : Int
        //  {
        //      return 0;
        //  }
        var tree = Program(
            Fun<IntType>("f").Parameter<IntType>("a").Parameter<IntType>("a").Body(
                Return(Literal(0))
            ));

        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<MultipleDeclarationsError>()));
    }

    [Fact]
    public void StructTypeInDeclarationRecognized()
    {
        // struct TestStruct {}
        // var a : *TestStruct;
        var tree = Program(
            Struct("TestStruct").Get(out var declaration),
            Var("a", Pointer(Ident("TestStruct", out var ident))
            ));

        var diagnostics = new Mock<IDiagnostics>();

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration, result.StructDeclarations[ident]);
    }

    [Fact]
    public void StructTypeInValueRecognized()
    {
        // struct TestStruct {}
        // var a = new(TestStruct{});
        var tree = Program(
            Struct("TestStruct").Get(out var declaration),
            Var("a", Alloc(StructValue(Ident("TestStruct", out var ident)))
            ));

        var diagnostics = new Mock<IDiagnostics>();

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration, result.StructDeclarations[ident]);
    }

    [Fact]
    public void StructTypeInReturnTypeRecognized()
    {
        // struct TestStruct {}
        // fun f() : *TestStruct {}
        var tree = Program(
            Struct("TestStruct").Get(out var declaration),
            Fun("f", Close, Pointer(Ident("TestStruct", out var ident)))
            );

        var diagnostics = new Mock<IDiagnostics>();

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration, result.StructDeclarations[ident]);
    }

    [Fact]
    public void StructTypeInArgumentRecognized()
    {
        // struct TestStruct {}
        // fun f(a : *TestStruct) : Unit {}
        var tree = Program(
            Struct("TestStruct").Get(out var declaration),
            Fun("f", Pointer(Ident("TestStruct", out var ident)), Close)
        );

        var diagnostics = new Mock<IDiagnostics>();

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration, result.StructDeclarations[ident]);
    }

    [Fact]
    public void StructTypeHiddenByMultiplePointersRecognized()
    {
        // struct TestStruct {}
        // var a : *(*(*TestStruct));
        var tree = Program(
            Struct("TestStruct").Get(out var declaration),
            Var("a", Pointer(Pointer(Pointer(Ident("TestStruct", out var ident))))
            ));

        var diagnostics = new Mock<IDiagnostics>();

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration, result.StructDeclarations[ident]);
    }

    [Fact]
    public void StructOvershadowedAndRecognized()
    {
        // struct TestStruct {}
        // {
        //   struct TestStruct {}
        //   var a : *TestStruct;
        // }
        var tree = Program(
            Struct("TestStruct"),
            Block(
                Struct("TestStruct").Get(out var declaration),
                Var("a", Pointer(Pointer(Pointer(Ident("TestStruct", out var ident))))
                )
            ));

        var diagnostics = new Mock<IDiagnostics>();

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration, result.StructDeclarations[ident]);
    }

    [Fact]
    public void StructsWithSameNameRecognized()
    {
        // {
        //   struct TestStruct {}
        //   var a : *TestStruct;
        // }
        // {
        //   struct TestStruct {}
        //   var a : *TestStruct;
        // }
        var tree = Program(
            Block(
                Struct("TestStruct").Get(out var declaration1),
                Var("a", Pointer(Pointer(Pointer(Ident("TestStruct", out var ident1))))
                ),
            Block(
                Struct("TestStruct").Get(out var declaration2),
                Var("a", Pointer(Pointer(Pointer(Ident("TestStruct", out var ident2))))
            )
            )));

        var diagnostics = new Mock<IDiagnostics>();

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration1, result.StructDeclarations[ident1]);
        Assert.Same(declaration2, result.StructDeclarations[ident2]);
    }

    [Fact]
    public void MultipleStructsCorrectlyResolved()
    {
        // struct Struct1 {}
        // struct Struct2 {}
        // struct Struct3 {}
        // var var3 : *Struct3;
        // var var1 : *Struct1;
        // var var2 : *Struct2;
        var tree = Program(
            Struct("Struct1").Get(out var struct1),
            Struct("Struct2").Get(out var struct2),
            Struct("Struct3").Get(out var struct3),
            Var("var3", Pointer(Ident("Struct3", out var ident3))),
            Var("var1", Pointer(Ident("Struct1", out var ident1))),
            Var("var2", Pointer(Ident("Struct2", out var ident2)))
        );

        var diagnostics = new Mock<IDiagnostics>();

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(struct1, result.StructDeclarations[ident1]);
        Assert.Same(struct2, result.StructDeclarations[ident2]);
        Assert.Same(struct3, result.StructDeclarations[ident3]);
    }

    [Fact]
    public void StructFieldsResolved()
    {
        // struct Struct1 {}
        // struct Struct2 {
        //   field: *Struct1
        // }
        var tree = Program(
            Struct("Struct1").Get(out var struct1),
            Struct("Struct2").Field("field", Pointer(Ident("Struct1", out var ident)))
        );

        var diagnostics = new Mock<IDiagnostics>();

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(struct1, result.StructDeclarations[ident]);
    }

    [Fact]
    public void SelfReferenceResolved()
    {
        // struct TestStruct {
        //   field: *TestStruct
        // }
        var tree = Program(
            Struct("TestStruct").Field("field", Pointer(Ident("TestStruct", out var ident))).Get(out var testStruct)
        );

        var diagnostics = new Mock<IDiagnostics>();

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(testStruct, result.StructDeclarations[ident]);
    }
    
    [Fact]
    public void IdentifierInStructValueResolved()
    {
        // struct TestStruct {
        //   field: Int
        // }
        // var a = new(
        //   TestStruct {
        //     field: (var x = 1; x)
        //   }
        // )
        var tree = Program(
            Struct("TestStruct").Field("field", Pointer(new IntType())),
            StructValue("TestStruct").Field("field", 
                Group(Var("x", 1, out var declaration), Value("x", out var value))
                )
        );

        var diagnostics = new Mock<IDiagnostics>();

        var result = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        Assert.Same(declaration, result.UsedVariableDeclarations[value]);
    }

    [Fact]
    public void StructDefinedInOtherScopeNotRecognized()
    {
        // {
        //   struct TestStruct {}
        // }
        // var a : *TestStruct;
        var tree = Program(
            Block(
                Struct("TestStruct")
                ),
            Var("a", Pointer(Pointer(Pointer(Ident("TestStruct"))))
            ));

        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<NotATypeError>()));
    }

    [Fact]
    public void StructNameCollisionDetected()
    {
        // struct TestStruct {}
        // struct TestStruct {}
        var tree = Program(
            Struct("TestStruct"),
            Struct("TestStruct")
            );

        var diagnostics = new Mock<IDiagnostics>();

        NameResolutionAlgorithm.Process(tree, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<MultipleDeclarationsError>()));
    }
}
