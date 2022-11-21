namespace sernickTest.Ast.Analysis;

using System.Collections.Immutable;
using Input;
using Moq;
using sernick.Ast;
using sernick.Ast.Analysis;
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

    public class TestAssignments
    {
        [Fact]
        public void CorrectLiteralAssignment_1()
        {
            var literal91 = Literal(91);
            var variableXDeclarationWithLiteralAssignment = Var<IntType>("x", literal91);
            var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

            var tree = Block(variableXDeclarationWithLiteralAssignment);
            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.Equal(new UnitType(), result[tree]);
            Assert.Equal(new UnitType(), result[variableXDeclarationWithLiteralAssignment]);
            Assert.Empty(diagnostics.Object.DiagnosticItems);
        }

        [Fact]
        public void IncorrectLiteralAssignment_1()
        {
            var literal91 = Literal(91);
            var variableXDeclarationWithLiteralAssignment = Var<BoolType>("x", literal91);
            var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);

            var tree = Block(variableXDeclarationWithLiteralAssignment);
            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.Equal(new UnitType(), result[tree]);
            Assert.Equal(new UnitType(), result[variableXDeclarationWithLiteralAssignment]);
            // I'm not sure if the next line is actually checking anything :(
            diagnostics.Verify(d => d.Report(It.IsAny<TypeCheckingError>()));
        }

        [Fact]
        public void IncorrectLiteralAssignment_2()
        {
            var literalFalse = Literal(false);
            var variableXDeclarationWithLiteralAssignment = Var<IntType>("x", literalFalse);
            var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
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
            var plusExpr = Plus(Literal(43), Literal(34));
            var tree = Program(plusExpr);

            var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.Equal(new IntType(), result[plusExpr]);
            Assert.Empty(diagnostics.Object.DiagnosticItems);
        }

        [Fact]
        public void AddingTwoBooleans_ERROR()
        {
            var minusExpr = Plus(Literal(true), Literal(false));
            var tree = Program(minusExpr);

            var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.Equal(new UnitType(), result[minusExpr]); // default in case of error
            Assert.NotEmpty(diagnostics.Object.DiagnosticItems); // maybe a more specific check here? TODO
        }

    }
}

  