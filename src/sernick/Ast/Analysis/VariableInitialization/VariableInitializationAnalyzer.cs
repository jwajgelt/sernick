namespace sernick.Ast.Analysis.VariableInitialization;

using System.Collections.Immutable;
using System.Diagnostics;
using CallGraph;
using Diagnostics;
using NameResolution;
using Nodes;
using Utility;
using VariableAccess;
using static FunctionDefinitionHierarchyAnalysis;

public static class VariableInitializationAnalyzer
{
    public static void Process(
        FunctionDefinition main,
        VariableAccessMap variableAccessMap,
        NameResolutionResult nameResolution,
        CallGraph callGraph,
        IDiagnostics diagnostics)
    {
        var functionHierarchy = FunctionDefinitionHierarchyAnalysis.Process(main);

        foreach (var functionDefinition in callGraph.Graph.Keys)
        {
            ProcessFunction(functionDefinition, functionHierarchy, variableAccessMap, nameResolution, callGraph, diagnostics);
        }
    }

    private static void ProcessFunction(
        FunctionDefinition function,
        FunctionHierarchy functionHierarchy,
        VariableAccessMap variableAccessMap,
        NameResolutionResult nameResolution,
        CallGraph callGraph,
        IDiagnostics diagnostics)
    {
        var enclosedFunctionsCallGraph = ClosureWithinScope(callGraph, function, functionHierarchy);
        var localVariables = LocalVariableDeclarations.Process(function.Body).ToHashSet();

        // for all enclosed functions, calculate the variable accesses to function's local variables
        var localVariableAccessMap = enclosedFunctionsCallGraph.Graph.ToDictionary(
            kv => kv.Key,
            kv => kv.Value
                .SelectMany(calledFunction => variableAccessMap[calledFunction])
                .SelectMany(variableAccess =>
                    variableAccess.Item1 is VariableDeclaration variableDeclaration
                        ? variableDeclaration.Enumerate()
                        : Enumerable.Empty<VariableDeclaration>())
                .Where(variable => localVariables.Contains(variable))
                .ToHashSet().AsEnumerable());

        try
        {
            function.Body.Accept(new VariableInitializationVisitor(nameResolution, localVariables, localVariableAccessMap),
                new VariableInitializationVisitorParam());
        }
        catch (VariableInitializationVisitorException e)
        {
            diagnostics.Report(e.Error);
        }
    }

    public abstract class VariableInitializationAnalysisError : IDiagnosticItem
    {
        public abstract override string ToString();
        public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
    }

    public class UninitializedVariableUseError : VariableInitializationAnalysisError
    {
        public UninitializedVariableUseError(VariableValue value)
        {
            _value = value;
        }

        public override string ToString() => $"Use of uninitialized variable {_value.Identifier.Name} at {_value.LocationRange}";

        private readonly VariableValue _value;
    }

    public class UninitializedNonLocalVariableUseError : VariableInitializationAnalysisError
    {
        public UninitializedNonLocalVariableUseError(VariableDeclaration variable, FunctionDefinition functionDefinition)
        {
            _variable = variable;
            _functionDefinition = functionDefinition;
        }

        public override string ToString() => $"Use of potentially uninitialized variable {_variable.Name} inside {_functionDefinition}";

        private readonly VariableDeclaration _variable;
        private readonly FunctionDefinition _functionDefinition;
    }

    public sealed class MultipleConstAssignmentError : VariableInitializationAnalysisError
    {
        public MultipleConstAssignmentError(Identifier identifier, Assignment? assignment = null)
        {
            _identifier = identifier;
            _assignment = assignment;
        }

        public override bool Equals(object? obj)
        {
            return obj is MultipleConstAssignmentError other && _identifier.Equals(other._identifier);
        }

        public override int GetHashCode()
        {
            return _identifier.GetHashCode();
        }

        public override string ToString() => $"Multiple assignment of const variable {_identifier.Name}" +
                                             (_assignment != null ? $" at {_identifier.LocationRange}" : "");

        private readonly Assignment? _assignment;
        private readonly Identifier _identifier;
    }

    private sealed record VariableInitializationVisitorParam
        (ImmutableHashSet<VariableDeclaration> InitializedVariables, ImmutableHashSet<VariableDeclaration> MaybeInitializedVariables, Assignment? CurrentlyAssignedIn)
    {
        public VariableInitializationVisitorParam() : this(ImmutableHashSet<VariableDeclaration>.Empty, ImmutableHashSet<VariableDeclaration>.Empty, null) { }
    }

    private sealed record VariableInitializationVisitorResult(
        ImmutableHashSet<VariableDeclaration> InitializedVariables, ImmutableHashSet<VariableDeclaration> MaybeInitializedVariables, bool BreaksLoop = false, bool Returns = false)
    {
        public bool Diverges => BreaksLoop || Returns;

        public VariableInitializationVisitorResult() : this(ImmutableHashSet<VariableDeclaration>.Empty, ImmutableHashSet<VariableDeclaration>.Empty) { }

        public VariableInitializationVisitorResult(VariableDeclaration declaration) : this(declaration.Enumerate()
            .ToImmutableHashSet(), ImmutableHashSet<VariableDeclaration>.Empty)
        {
            MaybeInitializedVariables = InitializedVariables;
        }
    }

    private sealed class VariableInitializationVisitorException : Exception
    {
        public VariableInitializationAnalysisError Error { get; }

        public VariableInitializationVisitorException(VariableInitializationAnalysisError error)
        {
            Error = error;
        }
    }

    private sealed class VariableInitializationVisitor
        : AstVisitor<VariableInitializationVisitorResult, VariableInitializationVisitorParam>
    {
        public VariableInitializationVisitor(
            NameResolutionResult nameResolution,
            HashSet<VariableDeclaration> localVariables,
            Dictionary<FunctionDefinition, IEnumerable<VariableDeclaration>> localVariableAccessMap)
        {
            _nameResolution = nameResolution;
            _localVariables = localVariables;
            _localVariableAccessMap = localVariableAccessMap;
        }

        protected override VariableInitializationVisitorResult VisitAstNode(AstNode node, VariableInitializationVisitorParam param)
        {
            var initializedVariables = ImmutableHashSet<VariableDeclaration>.Empty;
            var maybeInitializedVariables = ImmutableHashSet<VariableDeclaration>.Empty;
            var breaksLoop = false;
            var returns = false;

            foreach (var child in node.Children)
            {
                var childResult = child.Accept(
                    this,
                    new VariableInitializationVisitorParam(
                        param.InitializedVariables.Union(initializedVariables),
                        param.MaybeInitializedVariables.Union(maybeInitializedVariables),
                        param.CurrentlyAssignedIn));
                if (!returns && !breaksLoop)
                {
                    initializedVariables = initializedVariables.Union(childResult.InitializedVariables);
                }
                else
                {
                    maybeInitializedVariables = maybeInitializedVariables.Union(childResult.InitializedVariables);
                }

                maybeInitializedVariables = maybeInitializedVariables.Union(childResult.MaybeInitializedVariables);
                maybeInitializedVariables = maybeInitializedVariables.Union(initializedVariables);
                returns |= childResult.Returns;
                breaksLoop |= childResult.BreaksLoop;
            }

            return new VariableInitializationVisitorResult(initializedVariables, maybeInitializedVariables, breaksLoop, returns);
        }

        protected override VariableInitializationVisitorResult VisitLiteralValue(LiteralValue node, VariableInitializationVisitorParam param)
        {
            return new VariableInitializationVisitorResult();
        }

        public override VariableInitializationVisitorResult VisitIdentifier(Identifier node, VariableInitializationVisitorParam param)
        {
            return new VariableInitializationVisitorResult();
        }

        public override VariableInitializationVisitorResult VisitVariableDeclaration(VariableDeclaration node,
            VariableInitializationVisitorParam param)
        {
            return node.InitValue != null ? new VariableInitializationVisitorResult(node) : new VariableInitializationVisitorResult();
        }

        public override VariableInitializationVisitorResult VisitFunctionDefinition(FunctionDefinition node,
            VariableInitializationVisitorParam param)
        {
            return new VariableInitializationVisitorResult();
        }

        public override VariableInitializationVisitorResult VisitFunctionCall(FunctionCall node, VariableInitializationVisitorParam param)
        {
            var paramVisitResult = VisitAstNode(node, param);
            var calledFunction = _nameResolution.CalledFunctionDeclarations[node];

            if (!_localVariableAccessMap.ContainsKey(calledFunction))
            {
                return paramVisitResult;
            }

            // calling a local function
            foreach (var variable in _localVariableAccessMap[calledFunction])
            {
                if (!paramVisitResult.InitializedVariables.Contains(variable) &&
                    !param.InitializedVariables.Contains(variable))
                {
                    throw new VariableInitializationVisitorException(new UninitializedNonLocalVariableUseError(variable, calledFunction));
                }
            }

            return paramVisitResult;
        }

        public override VariableInitializationVisitorResult VisitContinueStatement(ContinueStatement node,
            VariableInitializationVisitorParam param)
        {
            return new VariableInitializationVisitorResult { BreaksLoop = true };
        }

        public override VariableInitializationVisitorResult
            VisitReturnStatement(ReturnStatement node, VariableInitializationVisitorParam param)
        {
            return (node.ReturnValue?.Accept(this, param) ?? new VariableInitializationVisitorResult())
                with
            { Returns = true };
        }

        public override VariableInitializationVisitorResult VisitBreakStatement(BreakStatement node, VariableInitializationVisitorParam param)
        {
            return new VariableInitializationVisitorResult { BreaksLoop = true };
        }

        public override VariableInitializationVisitorResult VisitIfStatement(IfStatement node, VariableInitializationVisitorParam param)
        {
            var conditionResult = node.Condition.Accept(this, param);

            var innerBlockParam = new VariableInitializationVisitorParam(
                param.InitializedVariables.Union(conditionResult.InitializedVariables),
                param.MaybeInitializedVariables.Union(conditionResult.MaybeInitializedVariables).Union(conditionResult.InitializedVariables),
                param.CurrentlyAssignedIn);

            var ifBranchResult = node.IfBlock.Accept(this, innerBlockParam);
            var elseBranchResult = (node.ElseBlock ?? new EmptyExpression(node.LocationRange) as AstNode).Accept(this, innerBlockParam);

            var initializedVariables =
                ifBranchResult.InitializedVariables.Intersect(elseBranchResult.InitializedVariables);
            var maybeInitializedVariables =
                ifBranchResult.MaybeInitializedVariables.Union(elseBranchResult.MaybeInitializedVariables);

            if (!conditionResult.Diverges)
            {
                return new VariableInitializationVisitorResult(
                    conditionResult.InitializedVariables.Union(initializedVariables),
                    conditionResult.MaybeInitializedVariables.Union(maybeInitializedVariables)
                );
            }

            return new VariableInitializationVisitorResult(
                conditionResult.InitializedVariables,
                conditionResult.MaybeInitializedVariables.Union(maybeInitializedVariables)
                    .Union(initializedVariables)
            );
        }

        public override VariableInitializationVisitorResult VisitLoopStatement(LoopStatement node, VariableInitializationVisitorParam param)
        {
            var bodyVisitResult = node.Inner.Accept(this, param);
            // NOTE: this is inefficient when nested loops are involved,
            // since each `loop` statement will visit the entire subtree,
            // but implementing this properly would require duplicating
            // name resolution logic for getting "variables visible in this scope".
            // If performance is an issue, we can throw in some memoization here,
            // to only ever visit each block (or loop) once.
            var localVariables = LocalVariableDeclarations.Process(node.Inner);

            foreach (var variable in bodyVisitResult.MaybeInitializedVariables.Where(variable => variable.IsConst).Except(localVariables))
            {
                throw new VariableInitializationVisitorException(new MultipleConstAssignmentError(variable.Name));
            }

            return bodyVisitResult with { BreaksLoop = false };
        }

        public override VariableInitializationVisitorResult VisitAssignment(Assignment node, VariableInitializationVisitorParam param)
        {
            if (node.Left is VariableValue value)
            {
                var assignedVariable = _nameResolution.UsedVariableDeclarations[value] as VariableDeclaration;
                Debug.Assert(assignedVariable is not null);

                if (assignedVariable.IsConst && param.MaybeInitializedVariables.Contains(assignedVariable))
                {
                    // check if there's multiple assignments to const
                    throw new VariableInitializationVisitorException(
                        new MultipleConstAssignmentError(value.Identifier, node));
                }

                return new VariableInitializationVisitorResult(assignedVariable);
            }

            var rightResult = VisitAstNode(node.Right, param);
            return VisitAstNode(node.Left, new VariableInitializationVisitorParam(
                param.InitializedVariables.Union(rightResult.InitializedVariables),
                param.MaybeInitializedVariables.Union(rightResult.MaybeInitializedVariables).Union(rightResult.InitializedVariables),
                node));
        }

        public override VariableInitializationVisitorResult VisitVariableValue(VariableValue node, VariableInitializationVisitorParam param)
        {
            if (param.CurrentlyAssignedIn is not null)
            {
                // bold assumption. We cannot be a simple assignment, because simple assignment is handled in VisitAssignment
                // therefore we are a struct of which field is being accessed
                var definition = _nameResolution.UsedVariableDeclarations[node] as VariableDeclaration;
                Debug.Assert(definition is not null);
                if (!param.InitializedVariables.Contains(definition))
                {
                    throw new VariableInitializationVisitorException(new UninitializedVariableUseError(node));
                }

                return new VariableInitializationVisitorResult();
            }

            if (_nameResolution.UsedVariableDeclarations[node] is VariableDeclaration variableDeclaration
                && _localVariables.Contains(variableDeclaration)
                && !param.InitializedVariables.Contains(variableDeclaration))
            {
                throw new VariableInitializationVisitorException(new UninitializedVariableUseError(node));
            }

            return new VariableInitializationVisitorResult();
        }

        public override VariableInitializationVisitorResult VisitPointerDereference(PointerDereference node,
            VariableInitializationVisitorParam param)
        {
            return base.VisitPointerDereference(node, param with { CurrentlyAssignedIn = null });
        }

        public override VariableInitializationVisitorResult
            VisitEmptyExpression(EmptyExpression node, VariableInitializationVisitorParam param)
        {
            return new VariableInitializationVisitorResult();
        }

        private readonly NameResolutionResult _nameResolution;
        private readonly HashSet<VariableDeclaration> _localVariables;
        private readonly Dictionary<FunctionDefinition, IEnumerable<VariableDeclaration>> _localVariableAccessMap;
    }

    /// <summary>
    /// Produces a (transitively closed) call graph of functions scoped within
    /// a given function, excluding calls to functions outside of that scope.
    /// </summary>
    private static CallGraph ClosureWithinScope(CallGraph callGraph, FunctionDefinition enclosingFunction, FunctionHierarchy functionHierarchy)
    {
        var enclosedFunctions = callGraph.Graph.Keys.Where(function => functionHierarchy.FunctionIsDescendantOf(function, enclosingFunction)).ToHashSet();

        var graph = enclosedFunctions.ToDictionary(function => function, function => new HashSet<FunctionDefinition> { function });

        foreach (var f in enclosedFunctions)
        {
            foreach (var g in callGraph.Graph[f])
            {
                if (enclosedFunctions.Contains(g))
                {
                    graph[f].Add(g);
                }
            }
        }

        return new CallGraph(graph.ToDictionary(
            kv => kv.Key,
            kv => kv.Value as IEnumerable<FunctionDefinition>
        )).Closure();
    }
}
