namespace sernickTest.Compiler.Function;

using sernick.CodeGeneration;
using sernick.Compiler.Function;
using static Ast.Helpers.AstNodesExtensions;

public class FunctionFactoryTest
{
    [Fact]
    public void GivesLabelsUsingGivenMethod()
    {
        var factory = new FunctionFactory((_, name, num) => new Label($"fun_{name.Name}_{num}"));

        var result = factory.CreateFunction(null, Ident("f"), 3, Array.Empty<IFunctionParam>(), true);

        Assert.Equal("fun_f_3", result.Label.Value);
    }
}
