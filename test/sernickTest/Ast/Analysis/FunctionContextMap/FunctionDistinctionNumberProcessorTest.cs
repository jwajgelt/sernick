namespace sernickTest.Ast.Analysis.FunctionContextMap;

using sernick.Ast;
using sernick.Ast.Analysis.FunctionContextMap;
using static Helpers.AstNodesExtensions;

public class FunctionDistinctionNumberProcessorTest
{
    [Fact]
    public void Process_main()
    {
        var tree = Program(Literal(1));
        var numberProvider = FunctionDistinctionNumberProcessor.Process(tree);

        Assert.Null(numberProvider(tree));
    }
    
    [Fact]
    public void Process_Nested()
    {
        var tree = Program(
            Fun<UnitType>("f").Body(
                Fun<UnitType>("g").Body(Literal(1)).Get(out var g)
                ).Get(out var f)
        );
        var numberProvider = FunctionDistinctionNumberProcessor.Process(tree);
        
        Assert.Null(numberProvider(tree));
        Assert.Null(numberProvider(f));
        Assert.Null(numberProvider(g));
    }
    
    [Fact]
    public void Process_NameCollision()
    {
        var tree = Program(
            If(Literal(true))
                .Then(Fun<UnitType>("f").Body(Literal(1)).Get(out var f1))
                .Else(Fun<UnitType>("f").Body(Literal(1)).Get(out var f2))
        );
        var numberProvider = FunctionDistinctionNumberProcessor.Process(tree);

        Assert.Null(numberProvider(tree));
        Assert.Equal(1, numberProvider(f1));
        Assert.Equal(2, numberProvider(f2));
    }
    
    [Fact]
    public void Process_DefinitionsInArguments()
    {
        var tree = Program(
            Fun<UnitType>("f").Body(Literal(1)).Get(out var f),
            "f".Call()
                .Argument(Block(Fun<IntType>("g").Body(Literal(1)).Get(out var g1), "g".Call()))
                .Argument(Block(Fun<IntType>("g").Body(Literal(1)).Get(out var g2), "g".Call()))
        );
        var numberProvider = FunctionDistinctionNumberProcessor.Process(tree);

        Assert.Null(numberProvider(tree));
        Assert.Null(numberProvider(f));
        Assert.Equal(1, numberProvider(g1));
        Assert.Equal(2, numberProvider(g2));
    }
    
}
