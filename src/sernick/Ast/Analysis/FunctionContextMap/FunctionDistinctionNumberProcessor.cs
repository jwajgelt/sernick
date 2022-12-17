namespace sernick.Ast.Analysis.FunctionContextMap;

using Nodes;
using Utility;

public static class FunctionDistinctionNumberProcessor
{
    public delegate int? DistinctionNumberProvider(FunctionDefinition definition);

    public static DistinctionNumberProvider Process(AstNode root)
    {
        return f => null;
        // var dict = new Dictionary<FunctionDefinition, int?>();
        // return f => dict[f];
    }
    
    
    private record PreprocessVisitorParam(IDictionary<FunctionDefinition, Multiset<string>> FunctionChildrenNames,
        FunctionDefinition? EnclosingFunction = null);
    private sealed class FunctionLabelPreprocessVisitor : AstVisitor<Unit, PreprocessVisitorParam>
    {
        protected override Unit VisitAstNode(AstNode node, PreprocessVisitorParam param)
        {
            foreach (var child in node.Children)
            {
                child.Accept(this, param);
            }
            return Unit.I;
        }

        public override Unit VisitFunctionDefinition(FunctionDefinition node, PreprocessVisitorParam param)
        {
            if (param.EnclosingFunction is not null)
            {
                if (!param.FunctionChildrenNames.ContainsKey(param.EnclosingFunction))
                {
                    param.FunctionChildrenNames[param.EnclosingFunction] = new Multiset<string>();
                }
                param.FunctionChildrenNames[param.EnclosingFunction].Add(node.Name.Name);
            }
            foreach (var parameter in node.Parameters)
            {
                parameter.Accept(this, param);
            }
            node.Body.Accept(this, param with { EnclosingFunction = node });
            return Unit.I;
        }
    }
}
