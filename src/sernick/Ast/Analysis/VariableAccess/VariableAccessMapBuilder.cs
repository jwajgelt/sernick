namespace sernick.Ast.Analysis.VariableAccess;

using NameResolution;
using sernick.Ast.Nodes;
using Utility;

public enum VariableAccessMode
{
    ReadOnly,
    WriteAndRead
}

public sealed class VariableAccessMap
{
    private readonly Dictionary<FunctionDefinition, Dictionary<Declaration, VariableAccessMode>>
        _variableAccessDictionary = new(
            ReferenceEqualityComparer.Instance);

    private readonly Dictionary<Declaration, FunctionDefinition?> _exclusiveWriteAccess =
        new(ReferenceEqualityComparer.Instance);

    /// <summary>
    ///     For a given function, it returns all variables accessed by this function
    ///     along with type of access (Read/Write)
    /// </summary>
    public IEnumerable<(Declaration, VariableAccessMode)> this[FunctionDefinition fun] =>
        _variableAccessDictionary[fun].Select(kv => (kv.Key, kv.Value));

    /// <summary>
    ///     Given function and a variable it checks whether this is the only function
    ///     with write access to the specified variable
    /// </summary>
    public bool HasExclusiveWriteAccess(FunctionDefinition fun, VariableDeclaration variable) =>
        _exclusiveWriteAccess.TryGetValue(variable, out var exclusiveFun) && ReferenceEquals(exclusiveFun, fun);
    

    internal void AddFun(FunctionDefinition fun)
    {
        _variableAccessDictionary[fun] = new Dictionary<Declaration, VariableAccessMode>(
            ReferenceEqualityComparer.Instance);
    }

    internal void AddVariableRead(FunctionDefinition fun, Declaration variable)
    {
        // If variable didn't have any access mode then it will get ReadOnly.
        // If variable had ReadOnly mode then it will still have ReadOnly.
        // If variable had WriteAndRead it should still have WriteAndRead because it is stronger.
        _variableAccessDictionary[fun].TryAdd(variable, VariableAccessMode.ReadOnly);
    }

    internal void AddVariableWrite(FunctionDefinition fun, Declaration variable)
    {
        // Overwrite access mode of variable to WriteAndRead because WriteAndRead is stronger.
        _variableAccessDictionary[fun][variable] = VariableAccessMode.WriteAndRead;

        _exclusiveWriteAccess[variable] = _exclusiveWriteAccess.ContainsKey(variable) ? null : fun;
    }
}

public static class VariableAccessMapPreprocess
{
    public static VariableAccessMap Process(AstNode ast, NameResolutionResult nameResolution)
    {
        var visitor = new VariableAccessVisitor(nameResolution);
        visitor.VisitAstTree(ast, null);
        return visitor.VariableAccess;
    }

    /// <summary>
    ///     AST visitor class used to extract info about way in which functions access variables.
    /// </summary>
    private sealed class VariableAccessVisitor : AstVisitor<Unit, FunctionDefinition?>
    {
        private readonly NameResolutionResult _nameResolution;

        public VariableAccessMap VariableAccess { get; }

        public VariableAccessVisitor(NameResolutionResult nameResolution)
        {
            _nameResolution = nameResolution;
            VariableAccess = new VariableAccessMap();
        }
        
        protected override Unit VisitAstNode(AstNode node, FunctionDefinition? currentFun)
        {
            foreach (var child in node.Children)
            {
                child.Accept(this, currentFun);
            }
            return Unit.I;
        }

        public override Unit VisitFunctionDefinition(FunctionDefinition funNode, FunctionDefinition? currentFun)
        {
            VariableAccess.AddFun(funNode);
            funNode.Body.Accept(this, funNode);
            return Unit.I;
        }

        public override Unit VisitVariableValue(VariableValue variableValue, FunctionDefinition? currentFun)
        {
            if (currentFun != null)
            {
                VariableAccess.AddVariableRead(currentFun, _nameResolution.UsedVariableDeclarations[variableValue]);
            }
            return Unit.I;
        }

        public override Unit VisitAssignment(Assignment assignment, FunctionDefinition? currentFun)
        {
            if (currentFun != null)
            {
                VariableAccess.AddVariableWrite(currentFun, _nameResolution.AssignedVariableDeclarations[assignment]);
            }
            return Unit.I;
        }
    }
}
