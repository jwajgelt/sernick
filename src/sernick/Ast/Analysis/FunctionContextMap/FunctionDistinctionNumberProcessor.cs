namespace sernick.Ast.Analysis.FunctionContextMap;

using Nodes;

public static class FunctionDistinctionNumberProcessor
{
    public delegate int? DistinctionNumberProvider(FunctionDefinition definition);

    public static DistinctionNumberProvider Process(AstNode root)
    {
        return f => null;
        // var dict = new Dictionary<FunctionDefinition, int?>();
        // return f => dict[f];
    }
}
