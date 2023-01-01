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
    public static void ProcessFunction(
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

        public override string ToString() => $"Use of uninitialized variable {_value.Identifier} at {_value.LocationRange}";

        private readonly VariableValue _value;
    }

    public class UninitializedNonLocalVariableUseError : VariableInitializationAnalysisError
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

    public class MultipleConstAssignmentError : VariableInitializationAnalysisError
    {
        public MultipleConstAssignmentError(VariableDeclaration declaration, Assignment? assignment = null)
        {
            _declaration = declaration;
            _assignment = assignment;
        }

        public override string ToString() => $"Multiple assignment of const variable {_declaration}" +
                                             (_assignment != null ? $" at {_assignment.LocationRange}" : "");

        private readonly Assignment? _assignment;
        private readonly VariableDeclaration _declaration;
    }

    private sealed record VariableInitializationVisitorParam
        (ImmutableHashSet<VariableDeclaration> InitializedVariables, ImmutableHashSet<VariableDeclaration> MaybeInitializedVariables)
    {
        public VariableInitializationVisitorParam() : this(ImmutableHashSet<VariableDeclaration>.Empty, ImmutableHashSet<VariableDeclaration>.Empty) { }
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
            Dictionary<FunctionDefinition, IEnumerable<VariableDeclaration>> localVariableAccessMap)
        {
            _nameResolution = nameResolution;
            _localVariableAccessMap = localVariableAccessMap;
        }

        protected override VariableInitializationVisitorResult VisitAstNode(AstNode node, VariableInitializationVisitorParam param)
        {
            var initializedVariables = param.InitializedVariables;
            var maybeInitializedVariables = param.MaybeInitializedVariables;
            var diverges = false;

            foreach (var child in node.Children)
            {
                var childResult = child.Accept(this, new VariableInitializationVisitorParam(initializedVariables, maybeInitializedVariables));
                if (diverges)
                {
                    initializedVariables = initializedVariables.Union(childResult.InitializedVariables);
                }
                else
                {
                    maybeInitializedVariables = maybeInitializedVariables.Union(childResult.InitializedVariables);
                }

                maybeInitializedVariables = maybeInitializedVariables.Union(childResult.MaybeInitializedVariables);
                maybeInitializedVariables = maybeInitializedVariables.Union(initializedVariables);
                diverges = diverges || childResult.Diverges;
            }

            return new VariableInitializationVisitorResult(initializedVariables, maybeInitializedVariables, diverges);
        }

        protected override VariableInitializationVisitorResult VisitLiteralValue(LiteralValue node, VariableInitializationVisitorParam param)
        {
            return new VariableInitializationVisitorResult();
        }

        public override VariableInitializationVisitorResult VisitVariableDeclaration(VariableDeclaration node,
            VariableInitializationVisitorParam param)
        {
            return node.InitValue != null ? new VariableInitializationVisitorResult(node) : new VariableInitializationVisitorResult();
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
            return new VariableInitializationVisitorResult { Returns = true };
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
                param.MaybeInitializedVariables.Union(conditionResult.MaybeInitializedVariables).Union(conditionResult.InitializedVariables));

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
                    conditionResult.MaybeInitializedVariables.Union(maybeInitializedVariables),
                    conditionResult.BreaksLoop || ifBranchResult.BreaksLoop || elseBranchResult.BreaksLoop,
                    conditionResult.Returns || ifBranchResult.Returns || elseBranchResult.Returns
                );
            }

            return new VariableInitializationVisitorResult(
                conditionResult.InitializedVariables,
                conditionResult.MaybeInitializedVariables.Union(maybeInitializedVariables)
                    .Union(initializedVariables),
                conditionResult.BreaksLoop || ifBranchResult.BreaksLoop || elseBranchResult.BreaksLoop,
                conditionResult.Returns || ifBranchResult.Returns || elseBranchResult.Returns
            );
        }

        public override VariableInitializationVisitorResult VisitLoopStatement(LoopStatement node, VariableInitializationVisitorParam param)
        {
            var bodyVisitResult = node.Inner.Accept(this, param);

            foreach (var variable in bodyVisitResult.MaybeInitializedVariables.Where(variable => variable.IsConst))
            {
                throw new VariableInitializationVisitorException(new MultipleConstAssignmentError(variable));
            }

            return bodyVisitResult with { BreaksLoop = false };
        }

        public override VariableInitializationVisitorResult VisitAssignment(Assignment node, VariableInitializationVisitorParam param)
        {
            var assignedVariable = _nameResolution.AssignedVariableDeclarations[node];

            if (assignedVariable.IsConst && param.MaybeInitializedVariables.Contains(assignedVariable))
            {
                // check if there's multiple assignments to const
                throw new VariableInitializationVisitorException(new MultipleConstAssignmentError(assignedVariable, node));
            }

            return new VariableInitializationVisitorResult(assignedVariable);
        }

        public override VariableInitializationVisitorResult VisitVariableValue(VariableValue node, VariableInitializationVisitorParam param)
        {
            if (_nameResolution.UsedVariableDeclarations[node] is VariableDeclaration variableDeclaration
                && !param.InitializedVariables.Contains(variableDeclaration))
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
