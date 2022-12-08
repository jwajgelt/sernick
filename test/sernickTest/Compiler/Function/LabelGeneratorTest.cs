namespace sernickTest.Compiler.Function;

using Moq;
using sernick.Compiler.Function;
using static Ast.Helpers.AstNodesExtensions;

public class LabelGeneratorTest
{
    [Fact]
    public void Generate_Case1()
    {
        var result = LabelGenerator.Generate(null, Ident("Shouldn't matter"));

        Assert.Equal("Main", result.Value);
    }

    [Fact]
    public void Generate_Case2()
    {
        var fMock = new Mock<IFunctionContext>();
        fMock.SetupGet(f => f.Label).Returns("Outer");

        var result = LabelGenerator.Generate(fMock.Object, Ident("Inner"));

        Assert.Equal("Outer_Inner", result.Value);
    }
}
