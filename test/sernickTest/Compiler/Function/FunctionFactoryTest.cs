namespace sernickTest.Compiler.Function;

using sernick.CodeGeneration;
using sernick.Compiler.Function;
using static Ast.Helpers.AstNodesExtensions;

public class FunctionFactoryTest
{
    [Fact]
    public void GivesLabelsUsingGivenMethod()
    {
        var factory = new FunctionFactory((_, name) => new Label($"fun_{name.Name}"));

        var result = factory.CreateFunction(null, Ident("f"), Array.Empty<IFunctionParam>(), true);

        Assert.Equal("fun_f", result.Label.Value);
    }
}
