namespace sernickTest.Ast.Analysis.VariableInitialization;

using Moq;
using sernick.Ast.Nodes;
using sernick.Ast.Analysis;
using sernick.Ast.Analysis.CallGraph;
using sernick.Ast.Analysis.VariableAccess;
using sernick.Ast.Analysis.VariableInitialization;
using sernick.Ast.Analysis.FunctionContextMap;
using sernick.Ast.Analysis.NameResolution;
using sernick.Compiler.Function;
using sernick.Diagnostics;
using static sernick.Ast.Analysis.VariableInitialization.VariableInitializationAnalyzer;
using static Helpers.AstNodesExtensions;

public class VariableInitializationAnalyzerTest
{
    [Fact]
    public void ErrorOnMultipleConstInitializations()
    {
        // const x = 1; x = 2;
        var tree = Program(
            Const("x", 1, out var declaration),
            "x".Assign(2, out var assignment)
        );

        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
        var callGraph = CallGraphBuilder.Process(tree, nameResolution);
        var variableAccessMap = VariableAccessMapPreprocess.Process(tree, nameResolution);

        var contextFactory = SetupFunctionFactory(out var mainContext);
        var functionContextMap = FunctionContextMapProcessor.Process(tree, nameResolution, _ => null, contextFactory.Object);

        VariableInitializationAnalyzer.ProcessFunction(tree, functionContextMap, variableAccessMap, nameResolution, callGraph, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<VariableInitializationAnalysisError>()));
    }

    [Fact]
    public void ErrorOnMultipleConstInitializations2()
    {
        // const x = 1; if(x == 1) { x = 2; }
        var tree = Program(
            Const("x", 1, out var declaration),
            If("x".Eq(Literal(1))).Then("x".Assign(2, out var assignment))
        );

        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
        var callGraph = CallGraphBuilder.Process(tree, nameResolution);
        var variableAccessMap = VariableAccessMapPreprocess.Process(tree, nameResolution);

        var contextFactory = SetupFunctionFactory(out var mainContext);
        var functionContextMap = FunctionContextMapProcessor.Process(tree, nameResolution, _ => null, contextFactory.Object);

        VariableInitializationAnalyzer.ProcessFunction(tree, functionContextMap, variableAccessMap, nameResolution, callGraph, diagnostics.Object);

        diagnostics.Verify(d => d.Report(It.IsAny<VariableInitializationAnalysisError>()));
    }

    [Fact]
    public void ErrorOnMultipleConstInitializations3()
    {
        // const x: Int; x = 1; x = 2;
    }

    [Fact]
    public void ErrorOnUninitializedUseInFunction()
    {
        // var x: Int;
        // const y: Int; 
        // fun foo(): Int { return x; };
        // fun bar(): Int { return y; }
        // foo(); bar();
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
    }

    [Fact]
    public void NoErrorOnInitializedUseInFunction()
    {
        // const x = 1;
        // const y = 2;
        // fun foo(): Int { return x; }; 
        // fun bar(): Int { return y; };
        // foo(); bar();

    }

    [Fact]
    public void NoErrorInitializationInIfBranches()
    {
        // const x: Bool;
        // var y: Int;
        // if(true) {x = true} else {x = false}
        // if(x) {y == 1} else {y = 2}
        // y = y + y
    }

    [Fact]
    public void NoErrorInitializationInAllNestedIfBranches()
    {
        // fun foo(a: Bool, b: Bool): Bool {
        //      const x: Bool;
        //      const y: Bool;
        //      if(a) {
        //         x = b;
        //         if(x) {
        //              y = true;
        //         } else {
        //              y = false;
        //         }  
        //      } else {
        //          y = !b;
        //          if(y) {
        //              x = y;
        //          } else {
        //              x = !y;
        //          }
        //      }
        //      return x || y;
        // }
    }

    [Fact]
    public void ErrorInitializationInSomeNestedIfBranches()
    {
        // fun foo(a: Bool, b: Bool): Bool {
        //      const x: Bool;
        //      const y: Bool;
        //      if(a) {
        //         x = b;
        //         if(x) {
        //              y = true;
        //         } else {
        //              y = false;
        //         }  
        //      } else {
        //          y = !b;
        //          if(y) {
        //              y = false;
        //          } else {
        //              x = !y;
        //          }
        //      }
        //      return x || y;
        // }
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
    }

    private static Mock<IFunctionFactory> SetupFunctionFactory(out IFunctionContext mainContext)
    {
        var contextFactory = new Mock<IFunctionFactory>();
        mainContext = new Mock<IFunctionContext>().Object;
        contextFactory
            .Setup(f => f.CreateFunction(null, It.IsAny<Identifier>(), It.IsAny<int?>(), Array.Empty<IFunctionParam>(), false))
            .Returns(mainContext);
        return contextFactory;
    }
}
