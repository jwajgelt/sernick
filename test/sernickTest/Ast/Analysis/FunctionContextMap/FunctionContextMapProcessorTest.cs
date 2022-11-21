namespace sernickTest.Ast.Analysis.FunctionContextMap;

using Castle.Core;
using Compiler.Function.Helpers;
using Moq;
using sernick.Ast;
using sernick.Ast.Analysis.FunctionContextMap;
using sernick.Ast.Analysis.NameResolution;
using sernick.Ast.Nodes;
using sernick.Compiler.Function;
using sernick.Utility;
using static Helpers.AstNodesExtensions;

public class FunctionContextMapProcessorTest
{
    [Fact]
    public void WhenFunctionDeclaredAndCalled_ThenCorrectContextIsCreated()
    {
        // fun f(a: Int) {}; f(0)
        var tree = Program(
            Fun<UnitType>("f")
                .Parameter<IntType>("a", out var declA)
                .Body(Close)
                .Get(out var declaration),
            "f".Call().Argument(Literal(0)).Get(out var call)
        );

        var contextFactory = SetupFunctionFactory(out var mainContext);
        var fContext = new Mock<IFunctionContext>().Object;
        contextFactory
            .Setup(f => f.CreateFunction(mainContext, new[] { new FunctionParamWrapper(declA) }, false))
            .Returns(fContext);

        var nameResolution = NameResolution(
            calledFunctions: new[] { (call, declaration) });
        var contextMap = FunctionContextMapProcessor.Process(tree, nameResolution, contextFactory.Object);

        Assert.Same(fContext, contextMap.Implementations[declaration]);
        Assert.Same(fContext, contextMap.Callers[call]);
    }

    [Fact]
    public void WhenFunctionDeclared_ThenCorrectLocalsAreAdded()
    {
        // fun f(a: Bool) { var x: Int; var y = 1; }; var z: Bool = false;
        var tree = Program(
            Fun<UnitType>("f")
                .Parameter<BoolType>("a", out var paramA)
                .Body(
                    Var<IntType>("x", out var declX),
                    Var("y", 1, out var declY)
                ),
            Var<BoolType>("z", false)
        );

        var contextFactory = SetupFunctionFactory(out var mainContext);
        var functionContext = new FakeFunctionContext();
        contextFactory
            .Setup(f => f.CreateFunction(mainContext, new[] { new FunctionParamWrapper(paramA) }, false))
            .Returns(functionContext);

        var nameResolution = NameResolution();
        FunctionContextMapProcessor.Process(tree, nameResolution, contextFactory.Object);

        Assert.False(functionContext.Locals[new FunctionVariableWrapper(paramA)]);
        Assert.False(functionContext.Locals[new FunctionVariableWrapper(declX)]);
        Assert.False(functionContext.Locals[new FunctionVariableWrapper(declY)]);
        Assert.True(3 == functionContext.Locals.Count);
    }

    [Fact]
    public void WhenNestedFunctionsDeclared_ThenUsedElsewhereComputedCorrectly()
    {
        // fun f(a: Int) {
        //   var y: Int;
        //   fun g() {
        //     var z: Int = a;
        //     var w: Int;
        //     w = y;
        //   }
        // }
        var tree = Program(
            Fun<UnitType>("f")
                .Parameter<IntType>("a", out var paramA)
                .Body(
                    Var<IntType>("y", out var declY),
                    Fun<UnitType>("g").Body(
                        Var<IntType>("z", Value("a", out var valueA), out var declZ),
                        Var<IntType>("w", out var declW),
                        "w".Assign(Value("y", out var valueY), out var assignment)
                    )
            )
        );

        var contextFactory = SetupFunctionFactory(out var mainContext);
        var fContext = new FakeFunctionContext();
        var gContext = new FakeFunctionContext();
        contextFactory
            .Setup(f => f.CreateFunction(mainContext, new[] { new FunctionParamWrapper(paramA) }, false))
            .Returns(fContext);
        contextFactory
            .Setup(f => f.CreateFunction(fContext, Array.Empty<FunctionParam>(), false))
            .Returns(gContext);

        var nameResolution = NameResolution(
            usedVariables: new[]
            {
                (valueA, paramA as Declaration),
                (valueY, declY as Declaration)
            },
            assignedVariables: new[] { (assignment, declW) });
        FunctionContextMapProcessor.Process(tree, nameResolution, contextFactory.Object);

        Assert.True(fContext.Locals[new FunctionVariableWrapper(paramA)]);
        Assert.True(fContext.Locals[new FunctionVariableWrapper(declY)]);
        Assert.True(2 == fContext.Locals.Count);

        Assert.False(gContext.Locals[new FunctionVariableWrapper(declZ)]);
        Assert.False(gContext.Locals[new FunctionVariableWrapper(declW)]);
        Assert.True(2 == gContext.Locals.Count);
    }

    [Fact]
    public void WhenLocalDeclaredInFunctionCall_ThenAddedToEnclosingFunction()
    {
        // fun f(a: Int) {
        //   f({var x: Int = a; x});
        // }
        var tree = Program(
            Fun<UnitType>("f")
                .Parameter<IntType>("a", out var paramA)
                .Body(
                    "f".Call()
                        .Argument(
                            Block(
                                Var<IntType>("x", Value("a", out var valueA), out var declX),
                                Value("x", out var valueX)
                            )
                        )
                        .Get(out var call)
                )
                .Get(out var funDeclaration)
        );

        var contextFactory = SetupFunctionFactory(out var mainContext);
        var functionContext = new FakeFunctionContext();
        contextFactory
            .Setup(f => f.CreateFunction(mainContext, new[] { new FunctionParamWrapper(paramA) }, false))
            .Returns(functionContext);

        var nameResolution = NameResolution(
            usedVariables: new[]
            {
                (valueA, paramA as Declaration),
                (valueX, declX as Declaration)
            },
            calledFunctions: new[] { (call, funDeclaration) });
        FunctionContextMapProcessor.Process(tree, nameResolution, contextFactory.Object);

        Assert.False(functionContext.Locals[new FunctionVariableWrapper(paramA)]);
        Assert.False(functionContext.Locals[new FunctionVariableWrapper(declX)]);
        Assert.True(2 == functionContext.Locals.Count);
    }

    [Fact]
    public void WhenNestedSiblingFunctionsDeclared_ThenLocalsAddedCorrectly()
    {
        // fun f() {
        //   var x: Int;
        //   fun g() {
        //     var x: Int;
        //   }
        //   fun h(): Int {
        //     var y: Int = x;
        //     0
        //   }
        // }
        var tree = Program(
            Fun<UnitType>("f").Body(
                Var<IntType>("x", out var declX),
                Fun<UnitType>("g").Body(
                    Var<IntType>("x", out var declXinG)
                ),
                Fun<IntType>("h").Body(
                    Var<IntType>("y", Value("x", out var valueX)),
                    Literal(0)
                )
            )
        );

        var contextFactory = SetupFunctionFactory(out var mainContext);
        var fContext = new FakeFunctionContext();
        var gContext = new FakeFunctionContext();
        var hContext = new FakeFunctionContext();
        contextFactory
            .Setup(f => f.CreateFunction(mainContext, Array.Empty<FunctionParam>(), false))
            .Returns(fContext);
        contextFactory
            .Setup(f => f.CreateFunction(fContext, Array.Empty<FunctionParam>(), false))
            .Returns(gContext);
        contextFactory
            .Setup(f => f.CreateFunction(fContext, Array.Empty<FunctionParam>(), true))
            .Returns(hContext);

        var nameResolution = NameResolution(
            usedVariables: new[] { (valueX, declX as Declaration) });
        FunctionContextMapProcessor.Process(tree, nameResolution, contextFactory.Object);

        Assert.False(gContext.Locals[new FunctionVariableWrapper(declXinG)]);
        Assert.Single(gContext.Locals);
    }

    [Fact]
    public void WhenVariableUsedInDeepNestedFunction()
    {
        // fun f() {
        //   var x: Int;
        //   fun g() {
        //     fun h() {
        //       var y: Int = x;
        //     }
        //   }
        // }
        var tree = Program(
            Fun<UnitType>("f").Body(
                Var<IntType>("x", out var declX),
                Fun<UnitType>("g").Body(
                    Fun<UnitType>("h").Body(
                        Var<IntType>("y", Value("x", out var valueX))
                    )
                )
            )
        );

        var contextFactory = SetupFunctionFactory(out var mainContext);
        var fContext = new FakeFunctionContext();
        var gContext = new FakeFunctionContext();
        var hContext = new FakeFunctionContext();
        contextFactory
            .Setup(f => f.CreateFunction(mainContext, Array.Empty<FunctionParam>(), false))
            .Returns(fContext);
        contextFactory
            .Setup(f => f.CreateFunction(fContext, Array.Empty<FunctionParam>(), false))
            .Returns(gContext);
        contextFactory
            .Setup(f => f.CreateFunction(gContext, Array.Empty<FunctionParam>(), false))
            .Returns(hContext);

        var nameResolution = NameResolution(
            usedVariables: new[] { (valueX, declX as Declaration) });
        FunctionContextMapProcessor.Process(tree, nameResolution, contextFactory.Object);

        Assert.True(fContext.Locals[new FunctionVariableWrapper(declX)]);
        Assert.Single(fContext.Locals);
    }

    private static NameResolutionResult NameResolution(
        IEnumerable<(VariableValue, Declaration)>? usedVariables = null,
        IEnumerable<(Assignment, VariableDeclaration)>? assignedVariables = null,
        IEnumerable<(FunctionCall, FunctionDefinition)>? calledFunctions = null)
    {
        var usedVariablesDict = usedVariables.OrEmpty()
            .ToDictionary(pair => pair.Item1, pair => pair.Item2,
                ReferenceEqualityComparer<VariableValue>.Instance);

        var assignedVariablesDict = assignedVariables.OrEmpty()
            .ToDictionary(pair => pair.Item1, pair => pair.Item2,
                ReferenceEqualityComparer<Assignment>.Instance);

        var calledFunctionsDict = calledFunctions.OrEmpty()
            .ToDictionary(pair => pair.Item1, pair => pair.Item2,
                ReferenceEqualityComparer<FunctionCall>.Instance);

        return new NameResolutionResult(usedVariablesDict, assignedVariablesDict, calledFunctionsDict);
    }

    private static Mock<IFunctionFactory> SetupFunctionFactory(out IFunctionContext mainContext)
    {
        var contextFactory = new Mock<IFunctionFactory>();
        mainContext = new Mock<IFunctionContext>().Object;
        contextFactory
            .Setup(f => f.CreateFunction(null, Array.Empty<FunctionParam>(), false))
            .Returns(mainContext);
        return contextFactory;
    }
}
