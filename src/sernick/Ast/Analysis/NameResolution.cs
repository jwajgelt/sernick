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
        var result = visitor.VisitAstTree(ast, new NameResolutionLocallyVisibleVariables());
        UsedVariableDeclarations = result.PartialResult.UsedVariableDeclarations;
        AssignedVariableDeclarations = result.PartialResult.AssignedVariableDeclarations;
        CalledFunctionDeclarations = result.PartialResult.CalledFunctionDeclarations;
    }

    
    private class NameResolvingAstVisitor : AstVisitor<NameResolutionVisitorResult, NameResolutionLocallyVisibleVariables>
    {
        private readonly IDiagnostics _diagnostics;
        public NameResolvingAstVisitor(IDiagnostics diagnostics)
        {
            _diagnostics = diagnostics;
        }
        protected override NameResolutionVisitorResult VisitAstNode(AstNode node, NameResolutionLocallyVisibleVariables variables)
        {
            var partialResult = new NameResolutionPartialResult();
            foreach (var child in node.Children)
            {
                var childResult = child.Accept(this, variables);
                partialResult = NameResolutionPartialResult.Join(partialResult, childResult.PartialResult);
                variables = childResult.Variables;
            }

            return new NameResolutionVisitorResult(partialResult, variables);
        }


        public override NameResolutionVisitorResult VisitVariableDeclaration(VariableDeclaration node, NameResolutionLocallyVisibleVariables variables)
        {
            var visibleVariables = variables.Add(node);
            return new NameResolutionVisitorResult(new NameResolutionPartialResult(), visibleVariables);
        }

        public override NameResolutionVisitorResult VisitFunctionParameterDeclaration(FunctionParameterDeclaration node,
            NameResolutionLocallyVisibleVariables variables)
        {
            var visibleVariables = variables.Add(node);
            return new NameResolutionVisitorResult(new NameResolutionPartialResult(), visibleVariables);
        }

        public override NameResolutionVisitorResult VisitFunctionDefinition(FunctionDefinition node, NameResolutionLocallyVisibleVariables variables)
        {
            var visibleVariables = variables.Add(node);
            var variablesInsideFunction = visibleVariables;
            foreach (var parameter in node.Parameters)
            {
                variablesInsideFunction = parameter.Accept(this, variablesInsideFunction).Variables;
            }

            var visitorResult = node.Body.Accept(this, variablesInsideFunction);
            
            return new NameResolutionVisitorResult(visitorResult.PartialResult, visibleVariables);
        }

        public override NameResolutionVisitorResult VisitCodeBlock(CodeBlock node, NameResolutionLocallyVisibleVariables variables)
        {
            var visitorResult = node.Inner.Accept(this, variables);
            return new NameResolutionVisitorResult(visitorResult.PartialResult, variables);
        }


        public override NameResolutionVisitorResult VisitFunctionCall(FunctionCall node, NameResolutionLocallyVisibleVariables variables)
        {
            var declaration = GetDeclarationAndReportIfMissing(node.FunctionName, variables);
            if (declaration == null)
            {
                return new NameResolutionVisitorResult(variables);
            }
            return new NameResolutionVisitorResult(NameResolutionPartialResult.OfCalledFunction(node, (FunctionDefinition)declaration),
                variables);
        }


        public override NameResolutionVisitorResult VisitAssignment(Assignment node, NameResolutionLocallyVisibleVariables variables)
        {
            var declaration = GetDeclarationAndReportIfMissing(node.Left, variables);
            if (declaration == null)
            {
                return new NameResolutionVisitorResult(variables);
            }
            return new NameResolutionVisitorResult(NameResolutionPartialResult.OfAssignment(node, (VariableDeclaration)declaration),
                variables);
        }

        public override NameResolutionVisitorResult VisitVariableValue(VariableValue node, NameResolutionLocallyVisibleVariables variables)
        {
            var declaration = GetDeclarationAndReportIfMissing(node.Identifier, variables);
            if (declaration == null)
            {
                return new NameResolutionVisitorResult(variables);
            }
            return new NameResolutionVisitorResult(NameResolutionPartialResult.OfUsedVariable(node, declaration),
                variables);
        }

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
