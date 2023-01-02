namespace sernick.Ast.Analysis.VariableAccess;

using System.Diagnostics;
using Diagnostics;
using NameResolution;
using Nodes;
using Utility;

public sealed class VariableAccessMap
{
    private readonly Dictionary<FunctionDefinition, Dictionary<Declaration, VariableAccessMode>>
        _variableAccessDictionary = new(ReferenceEqualityComparer.Instance);

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

        // `fun` has exclusive write access to `variable`
        // if `variable` hasn't been accessed by a different function
        // or it was accessed earlier by `fun`.
        var isNonExclusiveWrite = _exclusiveWriteAccess.TryGetValue(variable, out var exclusiveFun) &&
                                 !ReferenceEquals(exclusiveFun, fun);

        // If it was accessed by a different function set _exclusiveWriteAccess[variable] to null,
        // because no function will ever have an exclusive write access.
        // (Note that if we removed `variable` from exclusiveWriteAccess,
        //  next function accessing `variable` would get exclusive write access, which isn't correct)
        _exclusiveWriteAccess[variable] = isNonExclusiveWrite ? null : fun;
    }
}

public static class VariableAccessMapPreprocess
{
    /// <summary>
    ///     Constructs VariableAccessMap from AST and NameResolution.
    ///     The top node of AST should be a program node (main function declaration)
    /// </summary>
    public static VariableAccessMap Process(AstNode ast, NameResolutionResult nameResolution, IDiagnostics diagnostics)
    {
        var visitor = new VariableAccessVisitor(nameResolution, diagnostics);
        visitor.VisitAstTree(ast, null);
        return visitor.VariableAccess;
    }

    /// <summary>
    ///     AST visitor class used to extract info about way in which functions access variables.
    /// </summary>
    private sealed class VariableAccessVisitor : AstVisitor<Unit, FunctionDefinition?>
    {
        private readonly NameResolutionResult _nameResolution;
        private readonly Dictionary<VariableDeclaration, FunctionDefinition> _variableDeclaringFunction;
        private readonly IDiagnostics _diagnostics;

        public VariableAccessMap VariableAccess { get; }

        public VariableAccessVisitor(NameResolutionResult nameResolution, IDiagnostics diagnostics)
        {
            _nameResolution = nameResolution;
            _diagnostics = diagnostics;
            VariableAccess = new VariableAccessMap();
            _variableDeclaringFunction = new Dictionary<VariableDeclaration, FunctionDefinition>(ReferenceEqualityComparer.Instance);
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
            Debug.Assert(currentFun != null);
            VariableAccess.AddVariableRead(currentFun, _nameResolution.UsedVariableDeclarations[variableValue]);
            return Unit.I;
        }

        public override Unit VisitAssignment(Assignment assignment, FunctionDefinition? currentFun)
        {
            Debug.Assert(currentFun != null);
            var declaration = _nameResolution.AssignedVariableDeclarations[assignment];

            if (declaration.IsConst)
            {
                var declaringFunction = _variableDeclaringFunction[declaration];
                if (declaringFunction != currentFun)
                {
                    _diagnostics.Report(new InnerFunctionConstVariableWriteError(declaringFunction, declaration, currentFun, assignment));
                }
            }

            VariableAccess.AddVariableWrite(currentFun, declaration);
            return assignment.Right.Accept(this, currentFun);
        }

        public override Unit VisitVariableDeclaration(VariableDeclaration declaration, FunctionDefinition? currentFun)
        {
            Debug.Assert(currentFun != null);
            if (declaration.InitValue != null)
            {
                VariableAccess.AddVariableWrite(currentFun, declaration);
            }

            _variableDeclaringFunction[declaration] = currentFun;

            return declaration.InitValue?.Accept(this, currentFun) ?? Unit.I;
        }
    }
}
