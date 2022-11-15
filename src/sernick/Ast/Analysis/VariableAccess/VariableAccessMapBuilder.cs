namespace sernick.Ast.Analysis.VariableAccess;

using sernick.Ast.Nodes;

public enum VariableAccess 
{
    Read,
    Write
}

public sealed class VariableAccessMap
{
    public IEnumerable<(Declaration, VariableAccess)> GetFunctionsVariableAccesses(FunctionDefinition fun)
    {
        throw new NotImplementedException();
    }
}

public sealed class VariableAccessMapBuilder
{
    public static VariableAccessMap Process(AstNode ast) 
    {
        var visitor = new VariableAccessVisitor();
        return visitor.VisitAstTree(ast, new VariableAccessVisitorParam());
    }

    private sealed class VariableAccessVisitorParam {}

    /// <summary>
    ///     AST visitor class used to extract info about way in which functions access variables.
    /// </summary>
    private sealed class VariableAccessVisitor : AstVisitor<VariableAccessMap, VariableAccessVisitorParam>
    {    
        protected override VariableAccessMap VisitAstNode(AstNode node, VariableAccessVisitorParam param)
        {
            throw new NotImplementedException();
        }
    }
}