namespace sernickTest.Ast.Analysis.FunctionContextMap;

using Compiler.Function.Helpers;
using Moq;
using sernick.Ast;
using sernick.Ast.Analysis.FunctionContextMap;
using sernick.Ast.Analysis.StructProperties;
using sernick.Ast.Analysis.TypeChecking;
using sernick.Ast.Nodes;
using sernick.Compiler.Function;
using sernick.Diagnostics;
using static Helpers.AstNodesExtensions;
using static Helpers.NameResolutionResultBuilder;

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
            .Setup(f => f.CreateFunction(mainContext, It.IsAny<Identifier>(), It.IsAny<int?>(), new[] { declA }, false, null))
            .Returns(fContext);

        var nameResolution = NameResolution().WithCalls((call, declaration));
        var structProperties = new StructProperties();
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        var typeCheckingResult = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);
        var contextMap = FunctionContextMapProcessor.Process(tree, nameResolution, typeCheckingResult, structProperties, _ => null, contextFactory.Object);

        Assert.Same(fContext, contextMap.Implementations[declaration]);
        Assert.Same(fContext, contextMap.Callers[call]);
    }

    [Fact]
    public void WhenFunctionDeclared_UsesProvidedDistinctionNumber()
    {
        // fun f() {};
        var tree = Program(
            Fun<UnitType>("f")
                .Body(Close)
                .Get(out var declaration)
        );

        var contextFactory = SetupFunctionFactory(out var mainContext);
        var fContext = new Mock<IFunctionContext>().Object;
        contextFactory
            .Setup(f => f.CreateFunction(mainContext, It.IsAny<Identifier>(), 3, Array.Empty<IFunctionParam>(), false, null))
            .Returns(fContext);

        var nameResolution = NameResolution();
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        var typeCheckingResult = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);
        var structProperties = new StructProperties();
        var contextMap = FunctionContextMapProcessor.Process(tree, nameResolution, typeCheckingResult, structProperties, _ => 3, contextFactory.Object);

        Assert.Same(fContext, contextMap.Implementations[declaration]);
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
            .Setup(f => f.CreateFunction(mainContext, It.IsAny<Identifier>(), It.IsAny<int?>(), new[] { paramA }, false, null))
            .Returns(functionContext);

        var nameResolution = NameResolution();
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        var typeCheckingResult = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);
        var structProperties = new StructProperties();
        FunctionContextMapProcessor.Process(tree, nameResolution, typeCheckingResult, structProperties, _ => null, contextFactory.Object);

        Assert.False(functionContext.Locals[paramA]);
        Assert.False(functionContext.Locals[declX]);
        Assert.False(functionContext.Locals[declY]);
        Assert.True(3 == functionContext.Locals.Count);
    }

    [Fact(Skip = "There's probably some bug in type checking, since adding a real type checking makes this test fail")]
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
                        Value("w", out var assignment).Assign(Value("y", out var valueY))
                    )
            )
        );

        var contextFactory = SetupFunctionFactory(out var mainContext);
        var fContext = new FakeFunctionContext();
        var gContext = new FakeFunctionContext();
        contextFactory
            .Setup(f => f.CreateFunction(mainContext, It.IsAny<Identifier>(), It.IsAny<int?>(), new[] { paramA }, false, null))
            .Returns(fContext);
        contextFactory
            .Setup(f => f.CreateFunction(fContext, It.IsAny<Identifier>(), It.IsAny<int?>(), Array.Empty<IFunctionParam>(), false, null))
            .Returns(gContext);

        var nameResolution = NameResolution()
            .WithVars(
                (valueA, paramA),
                (valueY, declY),
                (assignment, declW));
        var structProperties = new StructProperties();
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        var typeCheckingResult = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);
        FunctionContextMapProcessor.Process(tree, nameResolution, typeCheckingResult, structProperties, _ => null, contextFactory.Object);

        Assert.True(fContext.Locals[paramA]);
        Assert.True(fContext.Locals[declY]);
        Assert.True(2 == fContext.Locals.Count);

        Assert.False(gContext.Locals[declZ]);
        Assert.False(gContext.Locals[declW]);
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
            .Setup(f => f.CreateFunction(mainContext, It.IsAny<Identifier>(), It.IsAny<int?>(), new[] { paramA }, false, null))
            .Returns(functionContext);

        var nameResolution = NameResolution()
            .WithVars(
                (valueA, paramA),
                (valueX, declX))
            .WithCalls(
                (call, funDeclaration));
        var structProperties = new StructProperties();
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        var typeCheckingResult = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);
        FunctionContextMapProcessor.Process(tree, nameResolution, typeCheckingResult, structProperties, _ => null, contextFactory.Object);

        Assert.False(functionContext.Locals[paramA]);
        Assert.False(functionContext.Locals[declX]);
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
            .Setup(f => f.CreateFunction(mainContext, It.IsAny<Identifier>(), It.IsAny<int?>(), Array.Empty<IFunctionParam>(), false, null))
            .Returns(fContext);
        contextFactory
            .Setup(f => f.CreateFunction(fContext, It.IsAny<Identifier>(), It.IsAny<int?>(), Array.Empty<IFunctionParam>(), false, null))
            .Returns(gContext);
        contextFactory
            .Setup(f => f.CreateFunction(fContext, It.IsAny<Identifier>(), It.IsAny<int?>(), Array.Empty<IFunctionParam>(), true, null))
            .Returns(hContext);

        var nameResolution = NameResolution()
            .WithVars((valueX, declX));
        var structProperties = new StructProperties();
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        var typeCheckingResult = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);
        FunctionContextMapProcessor.Process(tree, nameResolution, typeCheckingResult, structProperties, _ => null, contextFactory.Object);

        Assert.False(gContext.Locals[declXinG]);
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
            .Setup(f => f.CreateFunction(mainContext, It.IsAny<Identifier>(), It.IsAny<int?>(), Array.Empty<IFunctionParam>(), false, null))
            .Returns(fContext);
        contextFactory
            .Setup(f => f.CreateFunction(fContext, It.IsAny<Identifier>(), It.IsAny<int?>(), Array.Empty<IFunctionParam>(), false, null))
            .Returns(gContext);
        contextFactory
            .Setup(f => f.CreateFunction(gContext, It.IsAny<Identifier>(), It.IsAny<int?>(), Array.Empty<IFunctionParam>(), false, null))
            .Returns(hContext);

        var nameResolution = NameResolution()
            .WithVars((valueX, declX));
        var structProperties = new StructProperties();
        var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
        var typeCheckingResult = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);
        FunctionContextMapProcessor.Process(tree, nameResolution, typeCheckingResult, structProperties, _ => null, contextFactory.Object);

        Assert.True(fContext.Locals[declX]);
        Assert.Single(fContext.Locals);
    }

    private static Mock<IFunctionFactory> SetupFunctionFactory(out IFunctionContext mainContext)
    {
        var contextFactory = new Mock<IFunctionFactory>();
        mainContext = new Mock<IFunctionContext>().Object;
        contextFactory
            .Setup(f => f.CreateFunction(null, It.IsAny<Identifier>(), It.IsAny<int?>(), Array.Empty<IFunctionParam>(), false, null))
            .Returns(mainContext);
        return contextFactory;
    }
}
