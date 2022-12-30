namespace sernick.Ast.Analysis.VariableInitialization;

using System.Collections.Immutable;
using CallGraph;
using Diagnostics;
using FunctionContextMap;
using NameResolution;
using Nodes;
using Utility;
using VariableAccess;

public static class VariableInitializationAnalyzer
{
    public static void Process(
        FunctionDefinition function,
        FunctionContextMap functionContextMap,
        VariableAccessMap variableAccessMap,
        NameResolutionResult nameResolution,
        CallGraph callGraph,
        IDiagnostics diagnostics)
    {
        var enclosedFunctionsCallGraph = callGraph.ClosureWithinScope(function, functionContextMap);
        var localVariables = LocalVariableDeclarations.Process(function).ToHashSet();

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
            function.Accept(new VariableInitializationVisitor(nameResolution, localVariableAccessMap),
                new VariableInitializationVisitorParam());
        }
        catch (VariableInitializationVisitorException e)
        {
            diagnostics.Report(e.Error);
        }
    }

    private abstract class VariableInitializationAnalysisError : IDiagnosticItem
    {
        public abstract override string ToString();
        public DiagnosticItemSeverity Severity => DiagnosticItemSeverity.Error;
    }

    private class UninitializedVariableUseError : VariableInitializationAnalysisError
    {
        public UninitializedVariableUseError(VariableValue value)
        {
            _value = value;
        }

        public override string ToString() => $"Use of uninitialized variable {_value.Identifier} at {_value.LocationRange}";

        private readonly VariableValue _value;
    }

    private class UninitializedNonLocalVariableUseError : VariableInitializationAnalysisError
    {
        public UninitializedNonLocalVariableUseError(VariableDeclaration variable, FunctionDefinition functionDefinition)
        {
            _variable = variable;
            _functionDefinition = functionDefinition;
        }

        public override string ToString() => $"Use of potentially uninitialized variable {_variable} inside {_functionDefinition}";

        private readonly VariableDeclaration _variable;
        private readonly FunctionDefinition _functionDefinition;
    }

    private class MultipleConstAssignmentError : VariableInitializationAnalysisError
    {
        public MultipleConstAssignmentError(Assignment assignment)
        {
            _assignment = assignment;
        }

        public override string ToString() => $"Multiple assignment of const variable {_assignment.Left} at {_assignment.LocationRange}";

        private readonly Assignment _assignment;
    }

    private sealed record VariableInitializationVisitorParam
        (ImmutableHashSet<VariableDeclaration> initializedVariables, ImmutableHashSet<VariableDeclaration> maybeInitializedVariables)
    {
        public VariableInitializationVisitorParam() : this(ImmutableHashSet<VariableDeclaration>.Empty, ImmutableHashSet<VariableDeclaration>.Empty) { }
    }

    private sealed record VariableInitializationVisitorResult(
        ImmutableHashSet<VariableDeclaration> initializedVariables, ImmutableHashSet<VariableDeclaration> maybeInitializedVariables, bool diverges = false)
    {
        public VariableInitializationVisitorResult() : this(ImmutableHashSet<VariableDeclaration>.Empty, ImmutableHashSet<VariableDeclaration>.Empty) { }

        public VariableInitializationVisitorResult(VariableDeclaration declaration) : this(declaration.Enumerate()
            .ToImmutableHashSet(), ImmutableHashSet<VariableDeclaration>.Empty)
        {
            maybeInitializedVariables = initializedVariables;
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
            Dictionary<FunctionDefinition, IEnumerable<VariableDeclaration>> localVariableAccessMap)
        {
            _nameResolution = nameResolution;
            _localVariableAccessMap = localVariableAccessMap;
        }

        protected override VariableInitializationVisitorResult VisitAstNode(AstNode node, VariableInitializationVisitorParam param)
        {
            var initializedVariables = ImmutableHashSet<VariableDeclaration>.Empty;
            var maybeInitializedVariables = ImmutableHashSet<VariableDeclaration>.Empty;
            var diverges = false;

            foreach (var child in node.Children)
            {
                var childResult = VisitAstNode(child, new VariableInitializationVisitorParam(initializedVariables, maybeInitializedVariables));
                if (diverges)
                {
                    initializedVariables = initializedVariables.Union(childResult.initializedVariables);
                }
                else
                {
                    maybeInitializedVariables = maybeInitializedVariables.Union(childResult.initializedVariables);
                }

                maybeInitializedVariables = maybeInitializedVariables.Union(childResult.maybeInitializedVariables);
                maybeInitializedVariables = maybeInitializedVariables.Union(initializedVariables);
                diverges = diverges || childResult.diverges;
            }

            return new VariableInitializationVisitorResult(initializedVariables, maybeInitializedVariables, diverges);
        }

        protected override VariableInitializationVisitorResult VisitDeclaration(Declaration node, VariableInitializationVisitorParam param)
        {
            // this should be unreachable
            throw new NotSupportedException();
        }

        protected override VariableInitializationVisitorResult VisitSimpleValue(SimpleValue node, VariableInitializationVisitorParam param)
        {
            // this should be unreachable
            throw new NotSupportedException();
        }

        protected override VariableInitializationVisitorResult VisitLiteralValue(LiteralValue node, VariableInitializationVisitorParam param)
        {
            return new VariableInitializationVisitorResult();
        }

        public override VariableInitializationVisitorResult VisitIdentifier(Identifier node, VariableInitializationVisitorParam param)
        {
            // this should be unreachable
            throw new NotSupportedException();
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
                if (!paramVisitResult.initializedVariables.Contains(variable) &&
                    !param.initializedVariables.Contains(variable))
                {
                    throw new VariableInitializationVisitorException(new UninitializedNonLocalVariableUseError(variable, calledFunction));
                }
            }

            return paramVisitResult;
        }

        public override VariableInitializationVisitorResult VisitContinueStatement(ContinueStatement node,
            VariableInitializationVisitorParam param)
        {
            return new VariableInitializationVisitorResult { diverges = true };
        }

        public override VariableInitializationVisitorResult
            VisitReturnStatement(ReturnStatement node, VariableInitializationVisitorParam param)
        {
            return new VariableInitializationVisitorResult { diverges = true };
        }

        public override VariableInitializationVisitorResult VisitBreakStatement(BreakStatement node, VariableInitializationVisitorParam param)
        {
            return new VariableInitializationVisitorResult { diverges = true };
        }

        public override VariableInitializationVisitorResult VisitIfStatement(IfStatement node, VariableInitializationVisitorParam param)
        {
            throw new NotImplementedException("Check both branches, take the intersection of both. If either branch diverges, this statement diverges");
        }

        public override VariableInitializationVisitorResult VisitLoopStatement(LoopStatement node, VariableInitializationVisitorParam param)
        {
            throw new NotImplementedException("Check the body, and if the result assigns to a const, reject");
        }

        public override VariableInitializationVisitorResult VisitAssignment(Assignment node, VariableInitializationVisitorParam param)
        {
            var assignedVariable = _nameResolution.AssignedVariableDeclarations[node];

            if (assignedVariable.IsConst && param.maybeInitializedVariables.Contains(assignedVariable))
            {
                // check if there's multiple assignments to const
                throw new VariableInitializationVisitorException(new MultipleConstAssignmentError(node));
            }

            return new VariableInitializationVisitorResult(assignedVariable);
        }

        public override VariableInitializationVisitorResult VisitVariableValue(VariableValue node, VariableInitializationVisitorParam param)
        {
            if (_nameResolution.UsedVariableDeclarations[node] is VariableDeclaration variableDeclaration
                && !param.initializedVariables.Contains(variableDeclaration))
            {
                throw new VariableInitializationVisitorException(new UninitializedVariableUseError(node));
            }

            return new VariableInitializationVisitorResult();
        }

        public override VariableInitializationVisitorResult
            VisitEmptyExpression(EmptyExpression node, VariableInitializationVisitorParam param)
        {
            return new VariableInitializationVisitorResult();
        }

        private readonly NameResolutionResult _nameResolution;
        private readonly Dictionary<FunctionDefinition, IEnumerable<VariableDeclaration>> _localVariableAccessMap;
    }
}
