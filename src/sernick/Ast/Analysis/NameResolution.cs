namespace sernick.Ast.Analysis;

using Diagnostics;
using Nodes;
using Utility;

public sealed class NameResolution
{
    /// <summary>
    /// Maps uses of variables to the declarations
    /// of these variables
    /// </summary>
    public IReadOnlyDictionary<VariableValue, Declaration> UsedVariableDeclarations
    {
        get;
        init;
    }

    /// <summary>
    /// Maps left-hand sides of assignments to variables
    /// to the declarations of these variables.
    /// NOTE: Since function parameters are non-assignable,
    /// these can only be variable declarations (`var x`, `const y`)
    /// </summary>
    public IReadOnlyDictionary<Assignment, VariableDeclaration> AssignedVariableDeclarations
    {
        get;
        init;
    }

    /// <summary>
    /// Maps AST nodes for function calls
    /// to that function's declaration
    /// </summary>
    public IReadOnlyDictionary<FunctionCall, FunctionDefinition> CalledFunctionDeclarations
    {
        get;
        init;
    }

    public NameResolution(AstNode ast, IDiagnostics diagnostics)
    {
        var visitor = new NameResolvingAstVisitor(diagnostics);
        var result = visitor.VisitAstTree(ast, new NameResolutionVisitorParams());
        UsedVariableDeclarations = result.PartialResult.UsedVariableDeclarations;
        AssignedVariableDeclarations = result.PartialResult.AssignedVariableDeclarations;
        CalledFunctionDeclarations = result.PartialResult.CalledFunctionDeclarations;
    }

    
    private class NameResolvingAstVisitor : AstVisitor<NameResolutionVisitorResult, NameResolutionVisitorParams>
    {
        private readonly IDiagnostics _diagnostics;
        public NameResolvingAstVisitor(IDiagnostics diagnostics)
        {
            _diagnostics = diagnostics;
        }
        protected override NameResolutionVisitorResult VisitAstNode(AstNode node, NameResolutionVisitorParams param)
        {
            
            var variables = param.Variables;
            var partialResult = new NameResolutionPartialResult();
            foreach (var child in node.Children)
            {
                var childResult = child.Accept(this, new NameResolutionVisitorParams(variables));
                partialResult = NameResolutionPartialResult.Join(partialResult, childResult.PartialResult);
                variables = childResult.Variables;
            }

            return new NameResolutionVisitorResult(partialResult, variables);
        }

        // protected override NameResolutionVisitorResult VisitExpression(Expression node, NameResolutionVisitorParams param)
        // {
        //     return base.VisitExpression(node, param);
        // }

        // protected override NameResolutionVisitorResult VisitDeclaration(Declaration node, NameResolutionVisitorParams param)
        // {
        //     return base.VisitDeclaration(node, param);
        // }

        // protected override NameResolutionVisitorResult VisitFlowControlStatement(FlowControlStatement node, NameResolutionVisitorParams param)
        // {
        //     return base.VisitFlowControlStatement(node, param);
        // }

        // protected override NameResolutionVisitorResult VisitSimpleValue(SimpleValue node, NameResolutionVisitorParams param)
        // {
        //     return base.VisitSimpleValue(node, param);
        // }

        // protected override NameResolutionVisitorResult VisitLiteralValue(LiteralValue node, NameResolutionVisitorParams param)
        // {
        //     return base.VisitLiteralValue(node, param);
        // }

        // public override NameResolutionVisitorResult VisitIdentifier(Identifier node, NameResolutionVisitorParams param)
        // {
        //     return base.VisitIdentifier(node, param);
        // }

        public override NameResolutionVisitorResult VisitVariableDeclaration(VariableDeclaration node, NameResolutionVisitorParams param)
        {
            var visibleVariables = param.Variables.Add(node);
            return new NameResolutionVisitorResult(new NameResolutionPartialResult(), visibleVariables);
        }

        public override NameResolutionVisitorResult VisitFunctionParameterDeclaration(FunctionParameterDeclaration node,
            NameResolutionVisitorParams param)
        {
            var visibleVariables = param.Variables.Add(node);
            return new NameResolutionVisitorResult(new NameResolutionPartialResult(), visibleVariables);
        }

        public override NameResolutionVisitorResult VisitFunctionDefinition(FunctionDefinition node, NameResolutionVisitorParams param)
        {
            var visibleVariables = param.Variables.Add(node);
            var variablesInsideFunction = visibleVariables;
            foreach (var parameter in node.Parameters)
            {
                variablesInsideFunction = parameter.Accept(this, new NameResolutionVisitorParams(variablesInsideFunction)).Variables;
            }

            var visitorResult = node.Body.Accept(this, new NameResolutionVisitorParams(variablesInsideFunction));
            
            return new NameResolutionVisitorResult(visitorResult.PartialResult, visibleVariables);
        }

        public override NameResolutionVisitorResult VisitCodeBlock(CodeBlock node, NameResolutionVisitorParams param)
        {
            var visitorResult = node.Inner.Accept(this, param);
            return new NameResolutionVisitorResult(visitorResult.PartialResult, param.Variables);
        }

        // public override NameResolutionVisitorResult VisitExpressionJoin(ExpressionJoin node, NameResolutionVisitorParams param)
        // {
        //     var firstVisitorResult = node.First.Accept(this, param);
        //     var secondVisitorResult =
        //         node.Second.Accept(this, new NameResolutionVisitorParams(firstVisitorResult.Variables));
        //     return new NameResolutionVisitorResult(
        //         NameResolutionPartialResult.Join(firstVisitorResult.PartialResult, secondVisitorResult.PartialResult),
        //         secondVisitorResult.Variables);
        // }

        public override NameResolutionVisitorResult VisitFunctionCall(FunctionCall node, NameResolutionVisitorParams param)
        {
            var declaration = GetDeclarationAndReportIfMissing(node.FunctionName, param.Variables);
            if (declaration == null)
            {
                return new NameResolutionVisitorResult(param.Variables);
            }
            return new NameResolutionVisitorResult(NameResolutionPartialResult.OfCalledFunction(node, (FunctionDefinition)declaration),
                param.Variables);
        }

        // public override NameResolutionVisitorResult VisitContinueStatement(ContinueStatement node, NameResolutionVisitorParams param)
        // {
            // return base.VisitContinueStatement(node, param);
        // }

        // public override NameResolutionVisitorResult VisitReturnStatement(ReturnStatement node, NameResolutionVisitorParams param)
        // {
            // return base.VisitReturnStatement(node, param);
        // }

        // public override NameResolutionVisitorResult VisitBreakStatement(BreakStatement node, NameResolutionVisitorParams param)
        // {
            // return base.VisitBreakStatement(node, param);
        // }

        // public override NameResolutionVisitorResult VisitIfStatement(IfStatement node, NameResolutionVisitorParams param)
        // {
            // return base.VisitIfStatement(node, param);
        // }

        // public override NameResolutionVisitorResult VisitLoopStatement(LoopStatement node, NameResolutionVisitorParams param)
        // {
            // return base.VisitLoopStatement(node, param);
        // }

        // public override NameResolutionVisitorResult VisitInfix(Infix node, NameResolutionVisitorParams param)
        // {
            // return base.VisitInfix(node, param);
        // }

        public override NameResolutionVisitorResult VisitAssignment(Assignment node, NameResolutionVisitorParams param)
        {
            var declaration = GetDeclarationAndReportIfMissing(node.Left, param.Variables);
            if (declaration == null)
            {
                return new NameResolutionVisitorResult(param.Variables);
            }
            return new NameResolutionVisitorResult(NameResolutionPartialResult.OfAssignment(node, (VariableDeclaration)declaration),
                param.Variables);
        }

        public override NameResolutionVisitorResult VisitVariableValue(VariableValue node, NameResolutionVisitorParams param)
        {
            var declaration = GetDeclarationAndReportIfMissing(node.Identifier, param.Variables);
            if (declaration == null)
            {
                return new NameResolutionVisitorResult(param.Variables);
            }
            return new NameResolutionVisitorResult(NameResolutionPartialResult.OfUsedVariable(node, declaration),
                param.Variables);
        }

        // public override NameResolutionVisitorResult VisitBoolLiteralValue(BoolLiteralValue node, NameResolutionVisitorParams param)
        // {
        //     return base.VisitBoolLiteralValue(node, param);
        // }

        // public override NameResolutionVisitorResult VisitIntLiteralValue(IntLiteralValue node, NameResolutionVisitorParams param)
        // {
        //     return base.VisitIntLiteralValue(node, param);
        // }

        private Declaration? GetDeclarationAndReportIfMissing(Identifier identifier, NameResolutionLocallyVisibleVariables variables)
        {
            var name = identifier.Name;
            if (variables.Variables.ContainsKey(name))
            {
                return variables.Variables[name];
            }
            else
            {
                _diagnostics.Report(new UndeclaredIdentifierError(identifier));
                return null;
            }
        }
    }
}
