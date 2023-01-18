namespace sernickTest.Ast.Analysis;

using sernick.Ast;
using sernick.Ast.Analysis.NameResolution;
using sernick.Ast.Analysis.StructProperties;
using sernick.Ast.Nodes;
using sernickTest.Diagnostics;
using static Helpers.AstNodesExtensions;

public class StructPropertiesProcessorTest
{
    private static StructDeclaration DeclareStructList() => Struct("List")
        .Field("val", new IntType())
        .Field("next", new PointerType(new StructType(Ident("List"))));

    private static StructDeclaration DeclareStructTuple() => Struct("Tuple")
        .Field("intVal", new IntType())
        .Field("boolVal", new BoolType());

    private static StructDeclaration DeclareStructCombined() => Struct("Combined")
        .Field("list", new StructType(Ident("List")))
        .Field("intVal", new IntType())
        .Field("tuple", new StructType(Ident("Tuple")))
        .Field("next", new PointerType(new StructType(Ident("Combined"))));

    [Fact]
    public void TestSimple()
    {
        var listDeclaration = DeclareStructList();
        var tree = Block(
            listDeclaration
        );

        var diagnostics = new FakeDiagnostics();
        var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics);
        var result = StructPropertiesProcessor.Process(tree, nameResolution, diagnostics);

        Assert.Equal(16, result.StructSizes[listDeclaration]);
        Assert.Equal(0, result.FieldOffsets[listDeclaration.Fields.ElementAt(0)]);
        Assert.Equal(8, result.FieldOffsets[listDeclaration.Fields.ElementAt(1)]);
    }

    [Fact]
    public void TestStructTypeFields()
    {
        var listDeclaration = DeclareStructList();
        var tupleDeclaration = DeclareStructTuple();
        var combinedDeclaration = DeclareStructCombined();
        var tree = Block(
            listDeclaration, tupleDeclaration, combinedDeclaration
        );

        var diagnostics = new FakeDiagnostics();
        var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics);
        var result = StructPropertiesProcessor.Process(tree, nameResolution, diagnostics);

        Assert.Equal(16, result.StructSizes[listDeclaration]);
        Assert.Equal(0, result.FieldOffsets[listDeclaration.Fields.ElementAt(0)]);
        Assert.Equal(8, result.FieldOffsets[listDeclaration.Fields.ElementAt(1)]);

        Assert.Equal(16, result.StructSizes[tupleDeclaration]);
        Assert.Equal(0, result.FieldOffsets[tupleDeclaration.Fields.ElementAt(0)]);
        Assert.Equal(8, result.FieldOffsets[tupleDeclaration.Fields.ElementAt(1)]);

        Assert.Equal(48, result.StructSizes[combinedDeclaration]);
        Assert.Equal(0, result.FieldOffsets[combinedDeclaration.Fields.ElementAt(0)]);  // list
        Assert.Equal(16, result.FieldOffsets[combinedDeclaration.Fields.ElementAt(1)]); // int
        Assert.Equal(24, result.FieldOffsets[combinedDeclaration.Fields.ElementAt(2)]); // tuple
        Assert.Equal(40, result.FieldOffsets[combinedDeclaration.Fields.ElementAt(3)]); // pointer
    }

    [Fact]
    public void TestStructReferencingItself()
    {
        var declaration = Struct("S")
            .Field("s", new StructType(Ident("S")));
        var tree = Block(
            declaration
        );

        var diagnostics = new FakeDiagnostics();
        var nameResolution = NameResolutionAlgorithm.Process(tree, diagnostics);
        StructPropertiesProcessor.Process(tree, nameResolution, diagnostics);

        Assert.Contains(new StructPropertiesProcessorError("s", new StructType(Ident("S"))), diagnostics.DiagnosticItems);
    }
}
