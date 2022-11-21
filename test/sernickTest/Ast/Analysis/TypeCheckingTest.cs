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
        public void ExpressionWithSingleIntLiteral(){
            var literal23 = Literal(23);
            var tree = Program(
                literal23
            );
            var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.Same(new IntType(), result[literal23]);
            Assert.Same(new IntType(), result[tree]);
        }

        [Fact]
        public void ExpressionWithSingleBoolLiteral()
        {
            var literalTrue = Literal(true);
            var tree = Program(literalTrue);
            var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);


            Assert.Same(new BoolType(), result[literalTrue]);
            Assert.Same(new BoolType(), result[tree]);
        }

        [Fact]
        public void ExpressionWithChainedLiterals(){
            var literalFalse = Literal(false);
            var literal42 = Literal(42);
            var tree = Program(literalFalse, literal42);
            var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.Same(new BoolType(), result[literalFalse]);
            Assert.Same(new IntType(), result[literal42]);
            Assert.Same(new BoolType(), result[tree]);
        }

        [Fact]
        public void CorrectLiteralAssignment(){
            
        }
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

    private static LiteralValue GetBoolLiteral(bool b){
        return new BoolLiteralValue(b, loc);
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
