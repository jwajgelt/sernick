namespace sernickTest.Ast.Analysis;
using Moq;
using sernick.Ast;
using sernick.Ast.Analysis.NameResolution;
using sernick.Ast.Analysis.TypeChecking;
using sernick.Diagnostics;
using static Helpers.AstNodesExtensions;

public class TypeCheckingTest
{
    public class TestSimpleExpressions
    {
        [Fact]
        public void ExpressionWithSingleIntLiteral()
        {
            var literal23 = Literal(23);
            var tree = Block(
                literal23
            );

            var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

            var result = TypeCheckingResult.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.IsType<IntType>(result[literal23]);
            Assert.IsType<IntType>(result[tree]);
        }

        [Fact]
        public void ExpressionWithSingleBoolLiteral()
        {
            var literalTrue = Literal(true);
            var tree = Block(literalTrue);
            var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

            var result = TypeCheckingResult.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.IsType<BoolType>(result[literalTrue]);
            Assert.IsType<BoolType>(result[tree]);
        }

        [Fact]
        public void ExpressionWithChainedLiterals_1()
        {
            var literalFalse = Literal(false);
            var literal42 = Literal(42);
            var tree = Block(literalFalse, literal42);
            var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

            var result = TypeCheckingResult.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.IsType<BoolType>(result[literalFalse]);
            Assert.IsType<IntType>(result[literal42]);
            Assert.IsType<IntType>(result[tree]);
        }

        [Fact]
        public void ExpressionWithChainedLiterals_2()
        {
            var literal42 = Literal(42);
            var literalFalse = Literal(false);
            var tree = Block(literal42, literalFalse);
            var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

            var result = TypeCheckingResult.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.IsType<BoolType>(result[literalFalse]);
            Assert.IsType<IntType>(result[literal42]);
            Assert.IsType<BoolType>(result[tree]);
        }
    }

    public class TestVariableDeclaration
    {
        [Fact]
        public void Declaration_CorrectAssignment()
        {
            var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
            diagnostics.SetupAllProperties();

            var tree = Block(
              Var<IntType>("x", Literal(91), out var declX)
            );
            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeCheckingResult.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.IsType<UnitType>(result[tree]);
            Assert.IsType<UnitType>(result[declX]);
            Assert.Empty(diagnostics.Invocations);
        }

        [Fact]
        public void Declaration_WrongAssignment_1()
        {
            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var tree = Block(
              Var<BoolType>("x", Literal(91), out var declX)
            );
            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeCheckingResult.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.IsType<UnitType>(result[tree]);
            Assert.IsType<UnitType>(result[declX]);
            diagnostics.Verify(d => d.Report(It.IsAny<TypeCheckingErrorBase>()), Times.AtLeastOnce);
        }

        [Fact]
        public void Declaration_WrongAssignment_2()
        {
            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var tree = Block(
              Var<IntType>("x", Literal(false), out var declX)
            );
            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeCheckingResult.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.IsType<UnitType>(result[tree]);
            Assert.IsType<UnitType>(result[declX]);
            diagnostics.Verify(d => d.Report(It.IsAny<TypeCheckingErrorBase>()));
        }
    }

    public class TestInfix
    {
        [Fact]
        public void AddingTwoIntegerLiterals_OK()
        {
            var plusExpr = Literal(43).Plus(Literal(10));
            var tree = plusExpr;

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeCheckingResult.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.IsType<IntType>(result[plusExpr]);
            Assert.Empty(diagnostics.Invocations);
        }

        [Fact]
        public void AddingTwoBooleans_ERROR()
        {
            var plusExpr = Literal(true).Plus(Literal(false));
            var tree = plusExpr;

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeCheckingResult.CheckTypes(tree, nameResolution, diagnostics.Object);

            diagnostics.Verify(d => d.Report(It.IsAny<InfixOperatorTypeError>()), Times.AtLeastOnce);
        }

        [Fact]
        public void AddingIntAndBoolean_ERROR()
        {
            var plusExpr = Literal(true).Plus(Literal(123));
            var tree = plusExpr;

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeCheckingResult.CheckTypes(tree, nameResolution, diagnostics.Object);

            diagnostics.Verify(d => d.Report(It.IsAny<InfixOperatorTypeError>()), Times.AtLeastOnce);
        }

        [Fact]
        public void AddingTwoUnitExpressions_ERROR()
        {
            var plusExpr = Block(Var("x")).Plus(Block(Var("y")));
            var tree = plusExpr;

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeCheckingResult.CheckTypes(tree, nameResolution, diagnostics.Object);

            diagnostics.Verify(d => d.Report(It.IsAny<InfixOperatorTypeError>()), Times.AtLeastOnce);
        }

        [Fact]
        public void AddingIntAndUnit_ERROR()
        {
            var plusExpr = Literal(23).Plus(Block(Var("y")));
            var tree = Program(plusExpr);

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeCheckingResult.CheckTypes(tree, nameResolution, diagnostics.Object);

            diagnostics.Verify(d => d.Report(It.IsAny<InfixOperatorTypeError>()), Times.AtLeastOnce);
        }
    }

    public class TestLoop
    {
        [Fact]
        public void LoopNodeAlwaysReturnsUnit_1()
        {
            var tree = (
                Loop(
                    Block(
                        Literal(23),
                        Return(true)
                    )
                )
            );

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeCheckingResult.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.IsType<UnitType>(result[tree]);
            Assert.Empty(diagnostics.Object.DiagnosticItems);
        }

        [Fact]
        public void LoopNodeAlwaysReturnsUnit_2()
        {
            var tree = (
                Loop(
                    Block(
                        Literal(23),
                        Return(true),
                        Return(false),
                        Literal(91)
                    )
                )
            );

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeCheckingResult.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.IsType<UnitType>(result[tree]);
            Assert.Empty(diagnostics.Invocations);
        }
    }

    public class TestFunctionDeclaration
    {
        [Fact]
        public void FunctionReturnsBool_OK()
        {
            var tree = Fun<BoolType>("returnsBool")
                .Body(
                    Loop(
                        Block(
                            Literal(23),
                            Return(true)
                        )
                    ),
                    Literal(false)
                ).Get(out _);

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeCheckingResult.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.IsType<UnitType>(result[tree]);
            Assert.Empty(diagnostics.Invocations);
        }

        [Fact]
        public void FunctionReturnsBoolAndInt_BAD()
        {
            var funDecl = Fun<BoolType>("returnsBool");
            funDecl.Body(
                Loop(
                    Block(
                        Literal(23),
                        Return(91)
                    )
                ),
                Literal(false)
            );

            var tree = (
                funDecl
            );

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeCheckingResult.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.IsType<UnitType>(result[tree]);
            diagnostics.Verify(d => d.Report(It.IsAny<ReturnTypeError>()), Times.AtLeastOnce);

        }

        [Fact]
        public void FunctionReturnsInt_OK()
        {
            var funDecl = Fun<IntType>("returnsInt");
            funDecl.Body(
                Loop(
                    Block(
                        Literal(23),
                        Return(91)
                    )
                ),
                Literal(33)
            );

            var tree = (
                funDecl
            );

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeCheckingResult.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.IsType<UnitType>(result[tree]);
            Assert.Empty(diagnostics.Invocations);
        }

        [Fact]
        public void FunctionReturnsIntAndBool_bad()
        {
            var funDecl = Fun<IntType>("returnsInt");
            funDecl.Body(
                Loop(
                    Block(
                        Literal(23),
                        Return(false)
                    )
                ),
                Return(0),
                Literal(33)
            );

            var tree = (
                funDecl
            );

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeCheckingResult.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.IsType<UnitType>(result[tree]);
            diagnostics.Verify(d => d.Report(It.IsAny<ReturnTypeError>()), Times.AtLeastOnce);
        }
    }
}

