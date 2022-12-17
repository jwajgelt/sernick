namespace sernickTest.Compiler.Function;

using Moq;
using sernick.Compiler.Function;
using static Ast.Helpers.AstNodesExtensions;

public class LabelGeneratorTest
{
    [Fact]
    public void Generate_Main()
    {
        var result = LabelGenerator.Generate(null, Ident("Shouldn't matter"), null);

        Assert.Equal("Main", result.Value);
    }

    [Fact]
    public void Generate_NoDistinctionNumber()
    {
        var fMock = new Mock<IFunctionContext>();
        fMock.SetupGet(f => f.Label).Returns("Outer");

        var result = LabelGenerator.Generate(fMock.Object, Ident("Inner"), null);

        Assert.Equal("Outer.Inner", result.Value);
    }
    
    
    [Fact]
    public void Generate_WithDistinctionNumber()
    {
        var fMock = new Mock<IFunctionContext>();
        fMock.SetupGet(f => f.Label).Returns("Outer");

        var result = LabelGenerator.Generate(fMock.Object, Ident("Inner"), 3);

        Assert.Equal("Outer.Inner#3", result.Value);
    }
}
