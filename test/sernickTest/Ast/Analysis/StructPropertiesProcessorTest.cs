namespace sernickTest.Ast.Analysis;

using sernick.Ast;
using sernick.Ast.Analysis;
using sernick.Ast.Analysis.NameResolution;
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

    private static StructValue GetListDefault() => StructValue("List")
        .Field("val", Literal(0))
        .Field("next", Null);

    private static StructValue GetTupleDefault() => StructValue("Tuple")
        .Field("intVal", Literal(0))
        .Field("boolVal", Literal(false));

    private static StructValue GetCombinedDefault() => StructValue("Combined")
        .Field("list", GetListDefault())
        .Field("val", Literal(0))
        .Field("tuple", GetTupleDefault())
        .Field("next", Null);

    [Fact]
    public void TestSimple()
    {
        var listDeclaration = DeclareStructList();
        var tree = Block(
            listDeclaration
        );

        var nameResolution = NameResolutionAlgorithm.Process(tree, new FakeDiagnostics());
        var result = StructPropertiesProcessor.Process(tree, nameResolution);

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

        var nameResolution = NameResolutionAlgorithm.Process(tree, new FakeDiagnostics());
        var result = StructPropertiesProcessor.Process(tree, nameResolution);

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

        var nameResolution = NameResolutionAlgorithm.Process(tree, new FakeDiagnostics());
        Assert.ThrowsAny<Exception>(() => StructPropertiesProcessor.Process(tree, nameResolution));
    }
}