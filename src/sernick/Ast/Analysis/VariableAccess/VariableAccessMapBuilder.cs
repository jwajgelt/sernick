namespace sernick.Ast.Analysis.VariableAccess;

using sernick.Ast.Nodes;

public enum VariableAccessMode
{
    Read,
    Write
}

public sealed class VariableAccessMap
{
    /// <summary>
    ///     For a given function, it returns all variables accessed by this function
    ///     along with type of access (Read/Write)
    /// </summary>
    public IEnumerable<(Declaration, VariableAccessMode)> this[FunctionDefinition fun] =>
        throw new NotImplementedException();

    /// <summary>
    ///     Given function and a variable it checks whether this is the only function
    ///     with write access to the specified variable
    /// </summary>
    public bool HasExclusiveWriteAccess(FunctionDefinition fun, Declaration variable)
    {
        throw new NotImplementedException();
    }
}

public static class VariableAccessMapPreprocess
{
    public static VariableAccessMap Process(AstNode ast)
    {
        var visitor = new VariableAccessVisitor();
        return visitor.VisitAstTree(ast, new VariableAccessVisitorParam());
    }

    private sealed class VariableAccessVisitorParam { }

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
