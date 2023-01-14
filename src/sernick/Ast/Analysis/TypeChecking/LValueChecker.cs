namespace sernick.Ast.Analysis.TypeChecking;
using sernick.Ast.Analysis.NameResolution;
using sernick.Ast.Nodes;
using sernick.Utility;

public static class LValueChecker
{
    public static LValueCheckerResult Process(Expression lValue, NameResolutionResult nameResolutionResult, Dictionary<AstNode, Type> types)
    {
        var visitor = new LValueVisitor(nameResolutionResult, types);
        return lValue.Accept(visitor, Unit.I);
    }

    private class LValueVisitor : AstVisitor<LValueCheckerResult, Unit>
    {
        private readonly NameResolutionResult _nameResolution;
        private readonly Dictionary<AstNode, Type> _types;

        public LValueVisitor(NameResolutionResult nameResolution, Dictionary<AstNode, Type> types)
        {
            _nameResolution = nameResolution;
            _types = types;
        }

        protected override LValueCheckerResult VisitAstNode(AstNode node, Unit param)
        {
            return new LValueCheckerResult(IsLValue: false, IsConstStructAccess: false);
        }

        // Pointer dereference is always a valid L-Value
        // and it is never a L-Value to a const variable.
        // (We don't differentiate read-only pointers)
        public override LValueCheckerResult VisitPointerDereference(PointerDereference node, Unit param) =>
            new(IsLValue: true, IsConstStructAccess: false);

        public override LValueCheckerResult VisitVariableValue(VariableValue node, Unit param) =>
            new(IsLValue: true, IsConstStructAccess: false);

        public override LValueCheckerResult VisitStructFieldAccess(StructFieldAccess node, Unit param)
        {
            if (_types[node.Left] is PointerType)
            {
                return new LValueCheckerResult(IsLValue: true, IsConstStructAccess: false);
            }

            var leftResult = node.Left.Accept(this, Unit.I);
            return leftResult with
            {
                IsConstStructAccess = leftResult.IsConstStructAccess || IsConstVariable(node.Left)
            };
        }

        private bool IsConstVariable(AstNode expression)
        {
            if (expression is not VariableValue value)
            {
                return false;
            }

            if (_types[expression] is PointerType)
            {
                return false;
            }

            return _nameResolution.UsedVariableDeclarations[value] is VariableDeclaration { IsConst: true };
        }
    }
}

public record LValueCheckerResult(bool IsLValue, bool IsConstStructAccess);
