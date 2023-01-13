namespace sernickTest.Ast.Analysis;
using Input;
using Moq;
using sernick.Ast;
using sernick.Ast.Analysis.NameResolution;
using sernick.Ast.Analysis.TypeChecking;
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

            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

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

            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

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

            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

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
            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

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
            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

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
            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

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
            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

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
            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

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
            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

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
            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

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
            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

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
            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

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
            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

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
            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.IsType<UnitType>(result[tree]);
            Assert.Empty(diagnostics.Invocations);
        }

        [Fact]
        public void FunctionReturnsBoolAndInt_BAD()
        {
            var tree = Fun<BoolType>("returnsBool")
                .Body(
                Loop(
                    Block(
                        Literal(23),
                        Return(91)
                    )
                ),
                Literal(false)
            ).Get(out _);

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.IsType<UnitType>(result[tree]);
            diagnostics.Verify(d => d.Report(It.IsAny<ReturnTypeError>()), Times.AtLeastOnce);

        }

        [Fact]
        public void FunctionReturnsInt_OK()
        {
            var tree = Fun<IntType>("returnsInt")
                .Body(
                Loop(
                    Block(
                        Literal(23),
                        Return(91)
                    )
                ),
                Literal(33)
            ).Get(out _);

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.IsType<UnitType>(result[tree]);
            Assert.Empty(diagnostics.Invocations);
        }

        [Fact]
        public void FunctionReturnsIntAndBool_bad()
        {
            var tree = Fun<IntType>("returnsInt")
                .Body(
                Loop(
                    Block(
                        Literal(23),
                        Return(false)
                    )
                ),
                Return(0),
                Literal(33)
            ).Get(out _);

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.IsType<UnitType>(result[tree]);
            diagnostics.Verify(d => d.Report(It.IsAny<ReturnTypeError>()), Times.AtLeastOnce);
        }

        [Fact]
        public void FunctionExplicitReturnAndSemicolon()
        {
            /*
             * fun returnsInt(): Int {
             *   { 1 }
             *   return 0;
             * }
             */
            var tree = Fun<IntType>("returnsInt")
                .Body(
                    Block(Literal(1)),
                    Return(0), Close
                ).Get(out _);

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            diagnostics.Verify(d => d.Report(It.IsAny<TypeCheckingErrorBase>()), Times.Never);
        }

        [Fact]
        public void FunctionExplicitReturnOnSomeControlFlowPath()
        {
            /*
             * fun returnsInt(): Int {
             *   if (true) {
             *     0
             *   } else {
             *     return 1;
             *   }
             * }
             */
            var tree = Fun<IntType>("returnsInt")
                .Body(
                    If(true)
                        .Then(Literal(0))
                        .Else(Return(1), Close)
                ).Get(out _);

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            diagnostics.Verify(d => d.Report(It.IsAny<TypeCheckingErrorBase>()), Times.Never);
        }

        [Fact]
        public void FunctionExplicitReturnInBlock()
        {
            /*
             * fun returnsInt(): Int {
             *   { return 1; }
             * }
             */
            var tree = Fun<IntType>("returnsInt")
                .Body(
                    Block(Return(1), Close)
                ).Get(out _);

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            diagnostics.Verify(d => d.Report(It.IsAny<TypeCheckingErrorBase>()), Times.Never);
        }
    }
    public class TestPointers
    {
        [Fact]
        public void SimpleAllocations()
        {
            var allocInt = Alloc(Literal(42));
            var allocBool = Alloc(Literal(true));
            var allocPointer = Alloc(Alloc(Literal(0)));
            var tree = Block(
                allocInt, allocBool, allocPointer
            );

            var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.Equal(new PointerType(new IntType()), result[allocInt]);
            Assert.Equal(new PointerType(new BoolType()), result[allocBool]);
            Assert.Equal(new PointerType(new PointerType(new IntType())), result[allocPointer]);
        }

        [Fact]
        public void PointerDereference()
        {
            var derefInt = Deref(Alloc(Literal(1)));
            var derefPointer = Deref(Alloc(Alloc(Literal(false))));
            var tree = Block(
                derefInt, derefPointer
            );

            var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.Equal(new IntType(), result[derefInt]);
            Assert.Equal(new PointerType(new BoolType()), result[derefPointer]);
        }

        [Fact]
        public void NullPointerAssignment_OK()
        {
            var tree = Block(
                new VariableDeclaration(Ident("x"), new PointerType(new BoolType()), Null, false, loc),
                new VariableDeclaration(Ident("y"), new PointerType(new PointerType(new IntType())), Null, true, loc)
            );

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.Empty(diagnostics.Invocations);
        }

        [Fact]
        public void NullPointerAssignment_BAD()
        {
            var tree = Block(
                Var<IntType>("x", Null)
            );

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            diagnostics.Verify(d => d.Report(It.IsAny<TypesMismatchError>()), Times.AtLeastOnce);
        }

        [Fact]
        public void NullPointerAssignmentUnknownType()
        {
            var tree = Block(
                new VariableDeclaration(Ident("x"), null, Null, false, loc)
            );

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            diagnostics.Verify(d => d.Report(It.IsAny<TypeOrInitialValueShouldBePresentError>()), Times.AtLeastOnce);
        }

        [Fact]
        public void PointerDeclarationNoTypeSpecified()
        {
            // var ptr = new(1);
            var tree = Block(
                Var("ptr", Alloc(Literal(1)))
            );

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            diagnostics.VerifyNoOtherCalls();
        }

        [Fact]
        public void NullPointerInIfBranch()
        {
            // var ptr = new(1);
            // var x = if (1 <= 2) { null } else { ptr }
            var tree = Block(
                Var("ptr", Alloc(Literal(1))),
                Var("x", If(Literal(1).Leq(Literal(2)))
                    .Then(Null)
                    .Else(Value("ptr"))
                )
            );

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            diagnostics.VerifyNoOtherCalls();
        }

        [Fact]
        public void IllegalDereference()
        {
            // var ptr = 1
            // var val = *ptr
            var tree = Block(
                Var("ptr", Literal(1)),
                Var("val", Deref(Value("ptr")))
            );

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            diagnostics.Verify(d => d.Report(It.IsAny<CannotDereferenceExpressionError>()), Times.Once);
            diagnostics.VerifyNoOtherCalls();
        }
    }

    public class TestStruct
    {
        private static StructDeclaration DeclareStructList() => Struct("List")
            .Field("val", new IntType())
            .Field("next", new PointerType(new StructType(Ident("List"))));

        private static StructDeclaration DeclareStructTuple() => Struct("Tuple")
            .Field("intVal", new IntType())
            .Field("boolVal", new BoolType());

        private static StructDeclaration DeclareStructCombined() => Struct("Combined")
            .Field("list", new StructType(Ident("List")))
            .Field("tuple", new StructType(Ident("Tuple")));

        private static StructValue GetListDefault() => StructValue("List")
            .Field("val", Literal(0))
            .Field("next", Null);

        private static StructValue GetTupleDefault() => StructValue("Tuple")
            .Field("intVal", Literal(0))
            .Field("boolVal", Literal(false));

        private static StructValue GetCombinedDefault() => StructValue("Combined")
            .Field("list", GetListDefault())
            .Field("tuple", GetTupleDefault());

        [Fact]
        public void StructAllocationAndDereference()
        {
            var alloc = Alloc(GetCombinedDefault());
            var deref = Deref(alloc);
            var tree = Block(
                DeclareStructList(), DeclareStructTuple(), DeclareStructCombined(),
                deref
            );

            var diagnostics = new Mock<IDiagnostics>(MockBehavior.Strict);
            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);

            var result = TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.Equal(new PointerType(new StructType(Ident("Combined"))), result[alloc]);
            Assert.Equal(new StructType(Ident("Combined")), result[deref]);
        }

        [Fact]
        public void FieldAccessAndAssignment_OK()
        {
            var tree = Block(
                DeclareStructList(), DeclareStructTuple(), DeclareStructCombined(),
                new VariableDeclaration(Ident("x"), new StructType(Ident("Combined")), GetCombinedDefault(), false, loc),
                new VariableDeclaration(Ident("y"), new StructType(Ident("Tuple")), GetTupleDefault(), false, loc),
                Value("x").Field("tuple").Assign(GetTupleDefault()),
                Value("x").Field("list").Field("val").Assign(Literal(1)),
                Value("x").Field("list").Field("next").Assign(Alloc(GetListDefault())),
                Deref(Value("x").Field("list").Field("next")).Assign(GetListDefault()),
                Deref(Value("x").Field("list").Field("next")).Field("val").Assign(Literal(2)),
                Value("y").Field("boolVal").Assign(Literal(true))
            );
            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.Empty(diagnostics.Invocations);
        }

        [Fact]
        public void FieldAccessAutoDereference()
        {
            var tree = Block(
                DeclareStructList(), DeclareStructTuple(), DeclareStructCombined(),
                new VariableDeclaration(Ident("x"), new StructType(Ident("Combined")), GetCombinedDefault(), false, loc),
                Value("x").Field("list").Field("next").Field("next").Field("val").Assign(Literal(2))
            );

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.Empty(diagnostics.Invocations);
        }

        [Fact]
        public void FieldAccess_BAD()
        {
            var tree = Block(
                DeclareStructList(), DeclareStructTuple(), DeclareStructCombined(),
                new VariableDeclaration(Ident("x"), new StructType(Ident("Combined")), GetCombinedDefault(), false, loc),
                Value("x").Field("tuple").Field("val").Assign(Literal(1))
            );

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            diagnostics.Verify(d => d.Report(It.IsAny<FieldNotPresentInStructError>()), Times.AtLeastOnce);
        }

        [Fact]
        public void FieldAssignment_BAD()
        {
            var tree = Block(
                DeclareStructList(), DeclareStructTuple(), DeclareStructCombined(),
                new VariableDeclaration(Ident("x"), new StructType(Ident("Combined")), GetCombinedDefault(), false, loc),
                Value("x").Field("list").Field("next").Assign(GetListDefault())
            );

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            diagnostics.Verify(d => d.Report(It.IsAny<TypesMismatchError>()), Times.AtLeastOnce);
        }

        [Fact]
        public void NullPointerInStruct()
        {
            var tree = Block(
                DeclareStructList(), DeclareStructTuple(), DeclareStructCombined(),
                new VariableDeclaration(Ident("x"), new StructType(Ident("List")), GetListDefault(), false, loc),
                Value("x").Field("next").Assign(Null)
            );

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            Assert.Empty(diagnostics.Invocations);
        }

        [Fact]
        public void BadExpressionInTypeInitialization()
        {
            // struct TestStruct { field: Int };
            // var x = TestStruct { field: 1 < 2 };
            var tree = Block(
                Struct("TestStruct").Field("field", new IntType()),
                Var("x",
                    StructValue("TestStruct")
                        .Field("field", Literal(1).Leq(Literal(2)))
                    )
                );

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            diagnostics.Verify(d => d.Report(It.IsAny<TypesMismatchError>()), Times.Once);
        }

        [Fact]
        public void MissingFieldInitialization()
        {
            // struct Record = { field1: Int, field2: Int }
            // Record { field1: 1 };
            var tree = Block(
                Struct("Record")
                    .Field("field1", new IntType())
                    .Field("field2", new IntType()),
                StructValue("Record")
                    .Field("field1", Literal(1))
                );
            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            diagnostics.Verify(d => d.Report(It.IsAny<MissingFieldInitialization>()), Times.Once);
            diagnostics.VerifyNoOtherCalls();
        }

        [Fact]
        public void MultipleSameFieldInitialization()
        {
            // struct Record = { field: Int }
            // Record { field1: 1 };
            var tree = Block(
                Struct("Record").Field("field", new IntType()),
                StructValue("Record")
                    .Field("field", Literal(1))
                    .Field("field", Literal(2))
            );
            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            diagnostics.Verify(d => d.Report(It.IsAny<DuplicateFieldInitialization>()), Times.Once);
            diagnostics.VerifyNoOtherCalls();
        }

        [Fact]
        public void IllegalRecursiveStructType()
        {
            // struct Record = { field: Record }
            var tree = Block(
                Struct("Record")
                    .Field("field", new StructType(Ident("Record")))
                );

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            diagnostics.Verify(d => d.Report(It.IsAny<RecursiveStructDeclaration>()), Times.Once);
            diagnostics.VerifyNoOtherCalls();
        }

        [Fact]
        public void IllegalDuplicateFieldDeclaration()
        {
            var tree = Block(
                Struct("Record")
                    .Field("field", new IntType())
                    .Field("field", new IntType())
            );

            var diagnostics = new Mock<IDiagnostics>();
            diagnostics.SetupAllProperties();

            var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics.Object);
            TypeChecking.CheckTypes(tree, nameResolution, diagnostics.Object);

            diagnostics.Verify(d => d.Report(It.IsAny<DuplicateFieldDeclaration>()), Times.Once);
            diagnostics.VerifyNoOtherCalls();
        }
    }
}

