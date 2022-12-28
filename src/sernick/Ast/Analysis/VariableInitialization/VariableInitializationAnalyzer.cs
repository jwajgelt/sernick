namespace sernick.Ast.Analysis.VariableInitialization;

using System.Collections.Immutable;
using Diagnostics;
using NameResolution;
using Nodes;
using Utility;

public sealed class VariableInitializationAnalyzer
{
    public VariableInitializationAnalyzer()
    {

    }

    public abstract class VariableInitializationAnalysisError : IDiagnosticItem
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

    private sealed class
            VariableInitializationVisitor : AstVisitor<VariableInitializationVisitorResult,
                VariableInitializationVisitorParam>
    {
        public VariableInitializationVisitor(NameResolutionResult nameResolution)
        {
            _nameResolution = nameResolution;
        }

        protected override VariableInitializationVisitorResult VisitAstNode(AstNode node, VariableInitializationVisitorParam param)
        {
            // walk through children
            // when a child returns 'diverges', add further expressions' results as "maybe initialized"
            throw new NotImplementedException();
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
            throw new NotImplementedException("TODO: check accessed variables of function and throw if invalid");
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

            if (assignedVariable.IsConst && param.initializedVariables.Contains(assignedVariable))
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
    }
}
