namespace sernickTest.Ast.Analysis;

using System.Collections.Immutable;
using Input;
using Moq;
using sernick.Ast;
using sernick.Ast.Analysis;
using sernick.Ast.Analysis.TypeChecking;
using sernick.Ast.Analysis.NameResolution;
using sernick.Ast.Nodes;
using sernick.Diagnostics;
using sernick.Input;
using sernick.Utility;
using static Helpers.AstNodesExtensions;

public class TypeCheckingTest
{
    private static readonly Range<ILocation> loc = new(new FakeLocation(), new FakeLocation());

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

            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.Equal(new IntType(), result[literal23]);
            Assert.Equal(new IntType(), result[tree]);
        }

        [Fact]
        public void ExpressionWithSingleBoolLiteral()
        {
            var literalTrue = Literal(true);
            var tree = Block(literalTrue);
            var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);


            Assert.Equal(new BoolType(), result[literalTrue]);
            Assert.Equal(new BoolType(), result[tree]);
        }

        [Fact]
        public void ExpressionWithChainedLiterals_1()
        {
            var literalFalse = Literal(false);
            var literal42 = Literal(42);
            var tree = Block(literalFalse, literal42);
            var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.Equal(new BoolType(), result[literalFalse]);
            Assert.Equal(new IntType(), result[literal42]);
            Assert.Equal(new IntType(), result[tree]);
        }

        [Fact]
        public void ExpressionWithChainedLiterals_2()
        {
            var literal42 = Literal(42);
            var literalFalse = Literal(false);
            var tree = Block(literal42, literalFalse);
            var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.Equal(new BoolType(), result[literalFalse]);
            Assert.Equal(new IntType(), result[literal42]);
            Assert.Equal(new BoolType(), result[tree]);
        }
    }

    public class TestVariableDeclaration
    {
        [Fact]
        public void Declaration_CorrectAssignment()
        {
            var literal91 = Literal(91);
            var variableXDeclarationWithLiteralAssignment = Var<IntType>("x", literal91);
            var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
            diagnostics.SetupAllProperties();

            var tree = Block(variableXDeclarationWithLiteralAssignment);
            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.Equal(new UnitType(), result[tree]);
            Assert.Equal(new UnitType(), result[variableXDeclarationWithLiteralAssignment]);
            Assert.Empty(diagnostics.Object.DiagnosticItems);
        }

        [Fact]
        public void Declaration_WrongAssignment_1()
        {
            var literal91 = Literal(91);
            var variableXDeclarationWithLiteralAssignment = Var<BoolType>("x", literal91);
            var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
            diagnostics.SetupAllProperties();

            var tree = Block(variableXDeclarationWithLiteralAssignment);
            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.Equal(new UnitType(), result[tree]);
            Assert.Equal(new UnitType(), result[variableXDeclarationWithLiteralAssignment]);
            // I'm not sure if the next line is actually checking anything :(
            diagnostics.Verify(d => d.Report(It.IsAny<TypeCheckingError>()));
        }

        [Fact]
        public void Declaration_WrongAssignment_2()
        {
            var literalFalse = Literal(false);
            var variableXDeclarationWithLiteralAssignment = Var<IntType>("x", literalFalse);
            var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
            diagnostics.SetupAllProperties();

            var tree = Block(variableXDeclarationWithLiteralAssignment);
            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.Equal(new UnitType(), result[tree]);
            Assert.Equal(new UnitType(), result[variableXDeclarationWithLiteralAssignment]);
            // I'm not sure if the next line is actually checking anything :(
            diagnostics.Verify(d => d.Report(It.IsAny<TypeCheckingError>()));
        }
    }

    public class TestVariableValue
    {
        //[Fact]
        //public void IntVariableValue_1()
        //{
        //    var tree = Program(
        //        Var<IntType>("intX", Literal(23)),
        //        Value("intX")
        //    );
        //}

    }

    public class TestInfix
    {
        [Fact]
        public void AddingTwoIntegerLiterals_OK()
        {
            var plusExpr = Helpers.AstNodesExtensions.Plus(Literal(43), Literal(34));
            var tree = Program(plusExpr);

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.Equal(new IntType(), result[plusExpr]);
            Assert.Empty(diagnostics.Object.DiagnosticItems);
        }

        [Fact]
        public void AddingTwoBooleans_ERROR()
        {
            var plusExpr = Helpers.AstNodesExtensions.Plus(Literal(true), Literal(false));
            var tree = Program(plusExpr);

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            diagnostics.Verify(d => d.Report(It.IsAny<InfixOperatorTypeError>()), Times.AtLeastOnce);
        }

        [Fact]
        public void AddingIntAndBoolean_ERROR()
        {
            var plusExpr = Helpers.AstNodesExtensions.Plus(Literal(true), Literal(123));
            var tree = Program(plusExpr);

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            diagnostics.Verify(d => d.Report(It.IsAny<InfixOperatorTypeError>()), Times.AtLeastOnce);
        }

        [Fact]
        public void AddingTwoUnitExpressions_ERROR()
        {
            var plusExpr = Helpers.AstNodesExtensions.Plus(Block(Var("x")), Block(Var("y")));
            var tree = Program(plusExpr);

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            diagnostics.Verify(d => d.Report(It.IsAny<InfixOperatorTypeError>()), Times.AtLeastOnce);
        }

        [Fact]
        public void AddingIntAndUnit_ERROR()
        {
            var plusExpr = Helpers.AstNodesExtensions.Plus(Literal(23), Block(Var("y")));
            var tree = Program(plusExpr);

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            diagnostics.Verify(d => d.Report(It.IsAny<InfixOperatorTypeError>()), Times.AtLeastOnce);
        }
    }

    public class TestLoop {
        [Fact]
        public void TestLoopReturnInt_OK()
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
            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.Same(new IntType(), result[tree]);
            Assert.Empty(diagnostics.Object.DiagnosticItems);
        }
    }
}

  