namespace sernick.Ast.Analysis.FunctionContextMap;

using Nodes;
using Utility;

public static class FunctionDistinctionNumberProcessor
{
    public delegate int? DistinctionNumberProvider(FunctionDefinition definition);

    public static DistinctionNumberProvider Process(AstNode root)
    {
        var functionChildrenNames = new Dictionary<FunctionDefinition, Multiset<string>>();
        var preprocessVisitor = new FunctionLabelPreprocessVisitor();
        root.Accept(preprocessVisitor, new PreprocessVisitorParam(functionChildrenNames));
        var result = new Dictionary<FunctionDefinition, int?>();
        var visitor = new FunctionLabelVisitor(functionChildrenNames);
        root.Accept(visitor, new VisitorParam(result));
        return f => result[f];
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


    private record VisitorParam(
        IDictionary<FunctionDefinition, int?> DistinctionNumbers,
        Multiset<(FunctionDefinition, string)> ReoccurringFunctions,
        FunctionDefinition? EnclosingFunction = null)
    {
        public VisitorParam(Dictionary<FunctionDefinition, int?> distinctionNumbers) : this(distinctionNumbers, new Multiset<(FunctionDefinition, string)>())
        {
        }
    }

    private sealed class FunctionLabelVisitor : AstVisitor<Unit, VisitorParam>
    {
        private IDictionary<FunctionDefinition, Multiset<string>> _childrenNames;
        public FunctionLabelVisitor(IDictionary<FunctionDefinition, Multiset<string>> childrenNames)
        {
            _childrenNames = childrenNames;
        }
        
        protected override Unit VisitAstNode(AstNode node, VisitorParam param)
        {
            foreach (var child in node.Children)
            {
                child.Accept(this, param);
            }
            return Unit.I;
        }

        public override Unit VisitFunctionDefinition(FunctionDefinition node, VisitorParam param)
        {
            if (param.EnclosingFunction is null)
            {
                param.DistinctionNumbers[node] = null;
            }
            else
            {
                if (_childrenNames[param.EnclosingFunction].Get(node.Name.Name) > 1)
                {
                    var amount = param.ReoccurringFunctions.Get((param.EnclosingFunction, node.Name.Name));
                    param.DistinctionNumbers[node] = amount + 1;
                    param.ReoccurringFunctions.Add((param.EnclosingFunction, node.Name.Name));
                }
                else
                {
                    param.DistinctionNumbers[node] = null;
                }
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
