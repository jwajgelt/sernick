namespace sernickTest.Ast.Analysis.VariableInitialization;

using Moq;
using sernick.Ast;
using sernick.Ast.Analysis.CallGraph;
using sernick.Ast.Analysis.NameResolution;
using sernick.Ast.Analysis.VariableAccess;
using sernick.Ast.Analysis.VariableInitialization;
using sernick.Diagnostics;
using static Helpers.AstNodesExtensions;
using static sernick.Ast.Analysis.VariableInitialization.VariableInitializationAnalyzer;

public class VariableInitializationAnalyzerTest
{
    [Fact]
    public void ErrorOnMultipleConstInitializations()
    {
        // const x = 1; x = 2;
        var tree = Program(
            Const("x", 1),
            "x".Assign(2)
        );

        var diagnostics = new Mock<IDiagnostics>();
        var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
        var callGraph = CallGraphBuilder.Process(tree, nameResolution);
        var variableAccessMap = VariableAccessMapPreprocess.Process(tree, nameResolution, diagnostics.Object);

        VariableInitializationAnalyzer.Process(tree, variableAccessMap, nameResolution, callGraph, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<MultipleConstAssignmentError>()));
    }

    [Fact]
    public void ErrorOnMultipleConstInitializations2()
    {
        // const x = 1; if(false) { x = 2; }
        var tree = Program(
            Const("x", 1),
            If(Literal(false)).Then("x".Assign(2))
        );

        var diagnostics = new Mock<IDiagnostics>();
        var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
        var callGraph = CallGraphBuilder.Process(tree, nameResolution);
        var variableAccessMap = VariableAccessMapPreprocess.Process(tree, nameResolution, diagnostics.Object);

        VariableInitializationAnalyzer.Process(tree, variableAccessMap, nameResolution, callGraph, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<MultipleConstAssignmentError>()));
    }

    [Fact]
    public void ErrorOnMultipleConstInitializations3()
    {
        // const x: Int; x = 1; x = 2;
        var tree = Program(
            Const<IntType>("x"),
            "x".Assign(1),
            "x".Assign(2)
        );

        var diagnostics = new Mock<IDiagnostics>();
        var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
        var callGraph = CallGraphBuilder.Process(tree, nameResolution);
        var variableAccessMap = VariableAccessMapPreprocess.Process(tree, nameResolution, diagnostics.Object);

        VariableInitializationAnalyzer.Process(tree, variableAccessMap, nameResolution, callGraph, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<MultipleConstAssignmentError>()));
    }

    [Fact]
    public void ErrorOnUninitializedUseInFunction()
    {
        // var x: Int;
        // const y: Int; 
        // fun foo(): Int { return x; };
        // fun bar(): Int { return y; }
        // foo(); bar();
        var tree = Program(
            Var<IntType>("x"),
            Const<IntType>("y"),
            Fun<IntType>("foo").Body(Return(Value("x"))),
            Fun<IntType>("bar").Body(Return(Value("y"))),
            "foo".Call(),
            "bar".Call()
        );

        var diagnostics = new Mock<IDiagnostics>();
        var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
        var callGraph = CallGraphBuilder.Process(tree, nameResolution);
        var variableAccessMap = VariableAccessMapPreprocess.Process(tree, nameResolution, diagnostics.Object);

        VariableInitializationAnalyzer.Process(tree, variableAccessMap, nameResolution, callGraph, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<UninitializedNonLocalVariableUseError>()));
    }

    [Fact]
    public void ErrorOnMaybeUninitializedUseInFunction()
    {
        // var x: Int; 
        // const y: Int; 
        // if(true) { x = 1; } else { y = 2; }
        // fun foo(): Int { return x; }; 
        // fun bar(): Int { return y; };
        // foo(); bar();
        var tree = Program(
            Var<IntType>("x"),
            Const<IntType>("y"),
            If(Literal(true)).Then("x".Assign(1)).Else("y".Assign(2)),
            Fun<IntType>("foo").Body(Return(Value("x"))),
            Fun<IntType>("bar").Body(Return(Value("y"))),
            "foo".Call(),
            "bar".Call()
        );

        var diagnostics = new Mock<IDiagnostics>();
        var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
        var callGraph = CallGraphBuilder.Process(tree, nameResolution);
        var variableAccessMap = VariableAccessMapPreprocess.Process(tree, nameResolution, diagnostics.Object);

        VariableInitializationAnalyzer.Process(tree, variableAccessMap, nameResolution, callGraph, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<UninitializedNonLocalVariableUseError>()));
    }

    [Fact]
    public void NoErrorOnInitializedUseInFunction()
    {
        // const x = 1;
        // const y = 2;
        // fun foo(): Int { return x; }; 
        // fun bar(): Int { return y; };
        // foo(); bar();
        var tree = Program(
            Var("x", 1),
            Const("y", 2),
            Fun<IntType>("foo").Body(Return(Value("x"))),
            Fun<IntType>("bar").Body(Return(Value("y"))),
            "foo".Call(),
            "bar".Call()
        );

        var diagnostics = new Mock<IDiagnostics>();
        var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
        var callGraph = CallGraphBuilder.Process(tree, nameResolution);
        var variableAccessMap = VariableAccessMapPreprocess.Process(tree, nameResolution, diagnostics.Object);

        VariableInitializationAnalyzer.Process(tree, variableAccessMap, nameResolution, callGraph, diagnostics.Object);

        diagnostics.VerifyNoOtherCalls();
    }

    [Fact]
    public void NoErrorInitializationInIfBranches()
    {
        // const x: Bool;
        // var y: Int;
        // if(true) {x = true} else {x = false}
        // if(x) {y = 1} else {y = 2}
        // y = y + y
        var tree = Program(
            Const<BoolType>("x"),
            Var<IntType>("y"),
            If(Literal(true)).Then("x".Assign(Literal(true))).Else("x".Assign(Literal(false))),
            If(Value("x")).Then("y".Assign(1)).Else("y".Assign(2)),
            "y".Assign(Value("y").Plus(Value("y")))
        );

        var diagnostics = new Mock<IDiagnostics>();
        var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
        var callGraph = CallGraphBuilder.Process(tree, nameResolution);
        var variableAccessMap = VariableAccessMapPreprocess.Process(tree, nameResolution, diagnostics.Object);

        VariableInitializationAnalyzer.Process(tree, variableAccessMap, nameResolution, callGraph, diagnostics.Object);

        diagnostics.VerifyNoOtherCalls();
    }

    [Fact]
    public void NoErrorInitializationInAllNestedIfBranches()
    {
        // fun foo(a: Bool, b: Bool): Bool {
        //      var x: Bool;
        //      var y: Bool;
        //      if(a) {
        //         x = b;
        //         if(x) {
        //              y = true;
        //         } else {
        //              y = false;
        //         }  
        //      } else {
        //          y = (b == false);
        //          if(y) {
        //              x = y;
        //          } else {
        //              x = (y == false);
        //          }
        //      }
        //      return x || y;
        // }
        var tree = Program(
            Fun<BoolType>("foo").Parameter<BoolType>("a").Parameter<IntType>("b").Body(
                Var<BoolType>("x"),
                Var<BoolType>("y"),
                If(Value("a")).Then(
                    "x".Assign(Value("b")),
                    If(Value("x")).Then("y".Assign(Literal(true))).Else("y".Assign(Literal(false)))
                ).Else(
                    "y".Assign(Value("b").Eq(Literal(false))),
                    If(Value("y")).Then("x".Assign(Value("y"))).Else("x".Assign(Value("y").Eq(Literal(false))))
                ),
                Return(Value("x").ScOr(Value("y")))
            )
        );

        var diagnostics = new Mock<IDiagnostics>();
        var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
        var callGraph = CallGraphBuilder.Process(tree, nameResolution);
        var variableAccessMap = VariableAccessMapPreprocess.Process(tree, nameResolution, diagnostics.Object);

        VariableInitializationAnalyzer.Process(tree, variableAccessMap, nameResolution, callGraph, diagnostics.Object);

        diagnostics.VerifyNoOtherCalls();
    }

    [Fact]
    public void ErrorInitializationInSomeNestedIfBranches()
    {
        // fun foo(a: Bool, b: Bool): Bool {
        //      var x: Bool;
        //      var y: Bool;
        //      if(a) {
        //         x = b;
        //         if(x) {
        //              y = true;
        //         } else {
        //              y = false;
        //         }  
        //      } else {
        //          y = (b == false);
        //          if(y) {
        //              x = y;
        //          }
        //      }
        //      return x || y;
        // }
        var tree = Program(
            Fun<BoolType>("foo").Parameter<BoolType>("a").Parameter<IntType>("b").Body(
                Var<BoolType>("x"),
                Var<BoolType>("y"),
                If(Value("a")).Then(
                    "x".Assign(Value("b")),
                    If(Value("x")).Then("y".Assign(Literal(true))).Else("y".Assign(Literal(false)))
                ).Else(
                    "y".Assign(Value("b").Eq(Literal(false))),
                    If(Value("y")).Then("x".Assign(Value("y")))
                ),
                Return(Value("x").ScOr(Value("y")))
            )
        );

        var diagnostics = new Mock<IDiagnostics>();
        var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
        var callGraph = CallGraphBuilder.Process(tree, nameResolution);
        var variableAccessMap = VariableAccessMapPreprocess.Process(tree, nameResolution, diagnostics.Object);

        VariableInitializationAnalyzer.Process(tree, variableAccessMap, nameResolution, callGraph, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<UninitializedVariableUseError>()));
    }

    [Fact]
    public void ErrorConstInitializationInLoop()
    {
        // const x: Int;
        // loop {
        //      x = 1;
        //      if(x == 1) {
        //          break;
        //      }   
        // }
        var tree = Program(
            Const<IntType>("x"),
            Loop(
                "x".Assign(1),
                If(Value("x") == Literal(1)).Then(Break)
            )
        );

        var diagnostics = new Mock<IDiagnostics>();
        var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
        var callGraph = CallGraphBuilder.Process(tree, nameResolution);
        var variableAccessMap = VariableAccessMapPreprocess.Process(tree, nameResolution, diagnostics.Object);

        VariableInitializationAnalyzer.Process(tree, variableAccessMap, nameResolution, callGraph, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<MultipleConstAssignmentError>()));
    }

    [Fact]
    public void NoErrorScopedConstInitializationInLoop()
    {
        // loop {
        //      const x: Int;
        //      x = 1;
        //      if(x == 1) {
        //          break;
        //      }   
        // }
        var tree = Program(
            Loop(
                Const<IntType>("x"),
                "x".Assign(1),
                If(Value("x") == Literal(1)).Then(Break)
            )
        );

        var diagnostics = new Mock<IDiagnostics>();
        var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
        var callGraph = CallGraphBuilder.Process(tree, nameResolution);
        var variableAccessMap = VariableAccessMapPreprocess.Process(tree, nameResolution, diagnostics.Object);

        VariableInitializationAnalyzer.Process(tree, variableAccessMap, nameResolution, callGraph, diagnostics.Object);

        diagnostics.VerifyNoOtherCalls();
    }

    [Fact]
    public void NoErrorVarAssignmentInLoop()
    {
        // var x: Int;
        // loop {
        //      x = 1;
        //      if (x == 2) {
        //          break;
        //      }   
        // }
        var tree = Program(
            Var<IntType>("x"),
            Loop(
                "x".Assign(1),
                If(Value("x") == Literal(2)).Then(Break)
            )
        );

        var diagnostics = new Mock<IDiagnostics>();
        var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
        var callGraph = CallGraphBuilder.Process(tree, nameResolution);
        var variableAccessMap = VariableAccessMapPreprocess.Process(tree, nameResolution, diagnostics.Object);

        VariableInitializationAnalyzer.Process(tree, variableAccessMap, nameResolution, callGraph, diagnostics.Object);

        diagnostics.VerifyNoOtherCalls();
    }

    [Fact]
    public void NoErrorReentry()
    {
        // fn foo() {
        //      const x: Int;
        //      fn bar() {
        //          foo();
        //      }
        //      
        //      bar();
        //      x = 2;
        // }
        var tree = Program(
            Fun<UnitType>("foo").Body(
                Const<IntType>("x"),
                Fun<UnitType>("bar").Body(
                    "foo".Call()
                ),
                "bar".Call(),
                "x".Assign(2)
            )
        );

        var diagnostics = new Mock<IDiagnostics>();
        var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
        var callGraph = CallGraphBuilder.Process(tree, nameResolution);
        var variableAccessMap = VariableAccessMapPreprocess.Process(tree, nameResolution, diagnostics.Object);

        VariableInitializationAnalyzer.Process(tree, variableAccessMap, nameResolution, callGraph, diagnostics.Object);

        diagnostics.VerifyNoOtherCalls();
    }

    [Fact]
    public void ErrorReportedOnConstStructFieldAssignment()
    {
        // struct TestStruct {
        //   field: Int
        // }
        // const testStruct = TestStruct { field: 1};
        // testStruct.field = 2
        var tree = Program(
            Struct("TestStruct").Field("field", new IntType()),
            Const("testStruct", new StructType(Ident("TestStruct")),
                StructValue("TestStruct").Field("field", Literal(2))),
            Value("testStruct").Field("field").Assign(Literal(2))
        );

        var diagnostics = new Mock<IDiagnostics>();
        var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
        var callGraph = CallGraphBuilder.Process(tree, nameResolution);
        var variableAccessMap = VariableAccessMapPreprocess.Process(tree, nameResolution, diagnostics.Object);

        Process(tree, variableAccessMap, nameResolution, callGraph, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<ConstStructModifiedError>()));
    }

    [Fact]
    public void ErrorReportedOnUninitialisedStructFieldAssignment()
    {
        // struct TestStruct {
        //   field: Int
        // }
        // const testStruct : TestStruct;
        // testStruct.field = 2
        var tree = Program(
            Struct("TestStruct").Field("field", new IntType()),
            Const("testStruct", new StructType(Ident("TestStruct"))),
            Value("testStruct").Field("field").Assign(Literal(2))
        );

        var diagnostics = new Mock<IDiagnostics>();
        var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
        var callGraph = CallGraphBuilder.Process(tree, nameResolution);
        var variableAccessMap = VariableAccessMapPreprocess.Process(tree, nameResolution, diagnostics.Object);

        Process(tree, variableAccessMap, nameResolution, callGraph, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<UninitializedVariableUseError>()));
    }

    [Fact]
    public void ErrorReportedOnConstPointerAssignment()
    {
        // const a : *Int = 3; // using new is a huge problem in these tests, thankfully we don't do type checking
        // a = 1; 
        var tree = Program(
            Const("a", Pointer(new IntType()), Literal(3), out var declaration),
            Value("a").Assign(Literal(1))
        );
        Assert.NotNull(declaration);

        var diagnostics = new Mock<IDiagnostics>();
        var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
        var callGraph = CallGraphBuilder.Process(tree, nameResolution);
        var variableAccessMap = VariableAccessMapPreprocess.Process(tree, nameResolution, diagnostics.Object);

        Process(tree, variableAccessMap, nameResolution, callGraph, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<MultipleConstAssignmentError>()));
    }

    [Fact]
    public void NoErrorReportedOnConstPointerStructFieldAssignment()
    {
        // struct TestStruct {
        //   field: Int
        // }
        // const testStruct = 3; // omit new in tests
        // (*testStruct).field = 2
        var tree = Program(
            Struct("TestStruct").Field("field", new IntType()),
            Const("testStruct", Pointer(Ident("TestStruct")), Literal(3)),
            Deref(Value("testStruct")).Field("field").Assign(Literal(2))
        );

        var diagnostics = new Mock<IDiagnostics>();
        var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
        var callGraph = CallGraphBuilder.Process(tree, nameResolution);
        var variableAccessMap = VariableAccessMapPreprocess.Process(tree, nameResolution, diagnostics.Object);

        Process(tree, variableAccessMap, nameResolution, callGraph, diagnostics.Object);

        diagnostics.VerifyNoOtherCalls();
    }

    [Fact]
    public void ExpressionsInAssignmentsHandledCorrectly()
    {
        // var a : Bool;
        // var b : Bool;
        // var c = (a = (b = false; b); a);
        // var d = a || b || c;
        var tree = Program(
            Var<BoolType>("a"),
            Var<BoolType>("b"),
            Var("c", Block(
                Var("a", Block(
                    Value("b").Assign(Literal(false)),
                    Value("b")
                )),
                Value("a")
            )),
            Var("d", Value("a").ScOr(Value("b").ScOr(Value("c"))))
        );

        var diagnostics = new Mock<IDiagnostics>();
        var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
        var callGraph = CallGraphBuilder.Process(tree, nameResolution);
        var variableAccessMap = VariableAccessMapPreprocess.Process(tree, nameResolution, diagnostics.Object);

        Process(tree, variableAccessMap, nameResolution, callGraph, diagnostics.Object);

        diagnostics.VerifyNoOtherCalls();
    }
}
