namespace sernick.Ast.Analysis.FunctionContextMap;

using Nodes;
using Utility;

public static class FunctionDistinctionNumberProcessor
{
    public delegate int? DistinctionNumberProvider(FunctionDefinition definition);

    public static DistinctionNumberProvider Process(AstNode root)
    {
        var nameOccurrences = new Dictionary<FunctionDefinition, Multiset<string>>();
        var nameCountingVisitor = new NameOccurrencesVisitor();
        root.Accept(nameCountingVisitor, new NameCountingVisitorParam(nameOccurrences));
        var distinctionNumbers = new Dictionary<FunctionDefinition, int?>();
        var distinctionNumberVisitor = new DistinctionNumberVisitor(nameOccurrences);
        root.Accept(distinctionNumberVisitor, new DistinctionNumberVisitorParam(distinctionNumbers));
        return f => distinctionNumbers[f];
    }
    
    /// <param name="NameOccurrences">
    /// For every function, holds a set of function names defined in this function.
    /// This is a result, but is passed in param to avoid redundant dictionary merging.
    /// </param>
    /// <param name="EnclosingFunction">
    /// The function that is the closest ancestor of the current node.
    /// </param>
    private record NameCountingVisitorParam(
        IDictionary<FunctionDefinition, Multiset<string>> NameOccurrences,
        FunctionDefinition? EnclosingFunction = null);
    
    /// <summary>
    /// Lets us count occurrences of names in function body
    /// eg:
    /// <code>
    /// fun f() {
    ///   if (1>2) {
    ///     fun g() {}
    ///   } else {
    ///     fun g() {}
    ///   }
    /// }
    /// </code>
    /// in this code f would have two occurrences of "g"
    /// </summary>
    private sealed class NameOccurrencesVisitor : AstVisitor<Unit, NameCountingVisitorParam>
    {
        protected override Unit VisitAstNode(AstNode node, NameCountingVisitorParam param)
        {
            foreach (var child in node.Children)
            {
                child.Accept(this, param);
            }
            return Unit.I;
        }

        public override Unit VisitFunctionDefinition(FunctionDefinition node, NameCountingVisitorParam param)
        {
            if (param.EnclosingFunction is not null)
            {
                if (!param.NameOccurrences.ContainsKey(param.EnclosingFunction))
                {
                    param.NameOccurrences[param.EnclosingFunction] = new Multiset<string>();
                }
                param.NameOccurrences[param.EnclosingFunction].Add(node.Name.Name);
            }
            foreach (var parameter in node.Parameters)
            {
                parameter.Accept(this, param);
            }
            node.Body.Accept(this, param with { EnclosingFunction = node });
            return Unit.I;
        }
    }
    
    /// <param name="DistinctionNumbers">
    /// For every function, holds a distinction number of this function.
    /// This is a result, but is passed in param to avoid redundant dictionary merging.</param>
    /// <param name="NameOccurrencesEncountered">
    /// Holds information about function names already encountered so as to know
    /// what number should be given to next function with the same name.
    /// </param>
    /// <param name="EnclosingFunction">
    /// The function that is the closest ancestor of the current node.
    /// </param>
    private record DistinctionNumberVisitorParam(
        IDictionary<FunctionDefinition, int?> DistinctionNumbers,
        Multiset<(FunctionDefinition, string)> NameOccurrencesEncountered,
        FunctionDefinition? EnclosingFunction = null)
    {
        public DistinctionNumberVisitorParam(Dictionary<FunctionDefinition, int?> distinctionNumbers) : this(distinctionNumbers, new Multiset<(FunctionDefinition, string)>())
        {
        }
    }

    private sealed class DistinctionNumberVisitor : AstVisitor<Unit, DistinctionNumberVisitorParam>
    {
        private readonly IDictionary<FunctionDefinition, Multiset<string>> _childrenNames;
        public DistinctionNumberVisitor(IDictionary<FunctionDefinition, Multiset<string>> childrenNames)
        {
            _childrenNames = childrenNames;
        }
        
        protected override Unit VisitAstNode(AstNode node, DistinctionNumberVisitorParam param)
        {
            foreach (var child in node.Children)
            {
                child.Accept(this, param);
            }
            return Unit.I;
        }

        public override Unit VisitFunctionDefinition(FunctionDefinition node, DistinctionNumberVisitorParam param)
        {
            if (param.EnclosingFunction is null)
            {
                param.DistinctionNumbers[node] = null;
            }
            else
            {
                if (_childrenNames[param.EnclosingFunction].Get(node.Name.Name) > 1)
                {
                    var amount = param.NameOccurrencesEncountered.Get((param.EnclosingFunction, node.Name.Name));
                    param.DistinctionNumbers[node] = amount + 1;
                    param.NameOccurrencesEncountered.Add((param.EnclosingFunction, node.Name.Name));
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
