namespace sernick.Ast.Analysis.NameResolution;

using Diagnostics;
using Nodes;
using Utility;

public sealed class Algorithm
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

    public Algorithm(AstNode ast, IDiagnostics diagnostics)
    {
        var visitor = new NameResolvingAstVisitor(diagnostics);
        var result = visitor.VisitAstTree(ast, new NameResolutionLocallyVisibleVariables(diagnostics));
        UsedVariableDeclarations = result.PartialAlgorithmResult.UsedVariableDeclarations;
        AssignedVariableDeclarations = result.PartialAlgorithmResult.AssignedVariableDeclarations;
        CalledFunctionDeclarations = result.PartialAlgorithmResult.CalledFunctionDeclarations;
    }

    
    private class NameResolvingAstVisitor : AstVisitor<VisitorResult, NameResolutionLocallyVisibleVariables>
    {
        private readonly IDiagnostics _diagnostics;
        public NameResolvingAstVisitor(IDiagnostics diagnostics)
        {
            _diagnostics = diagnostics;
        }
        protected override VisitorResult VisitAstNode(AstNode node, NameResolutionLocallyVisibleVariables variables)
        {
            var partialResult = new PartialAlgorithmResult();
            foreach (var child in node.Children)
            {
                var childResult = child.Accept(this, variables);
                partialResult = PartialAlgorithmResult.Join(partialResult, childResult.PartialAlgorithmResult);
                variables = childResult.Variables;
            }

            return new VisitorResult(partialResult, variables);
        }


        public override VisitorResult VisitVariableDeclaration(VariableDeclaration node, NameResolutionLocallyVisibleVariables variables)
        {
            var visibleVariables = variables.Add(node);
            return new VisitorResult(new PartialAlgorithmResult(), visibleVariables);
        }

        public override VisitorResult VisitFunctionParameterDeclaration(FunctionParameterDeclaration node,
            NameResolutionLocallyVisibleVariables variables)
        {
            var visibleVariables = variables.Add(node);
            return new VisitorResult(new PartialAlgorithmResult(), visibleVariables);
        }

        public override VisitorResult VisitFunctionDefinition(FunctionDefinition node, NameResolutionLocallyVisibleVariables variables)
        {
            var visibleVariables = variables.Add(node);
            var variablesInsideFunction = visibleVariables;
            foreach (var parameter in node.Parameters)
            {
                variablesInsideFunction = parameter.Accept(this, variablesInsideFunction).Variables;
            }

            var visitorResult = node.Body.Accept(this, variablesInsideFunction);
            
            return new VisitorResult(visitorResult.PartialAlgorithmResult, visibleVariables);
        }

        public override VisitorResult VisitCodeBlock(CodeBlock node, NameResolutionLocallyVisibleVariables variables)
        {
            var visitorResult = node.Inner.Accept(this, variables);
            return new VisitorResult(visitorResult.PartialAlgorithmResult, variables);
        }


        public override VisitorResult VisitFunctionCall(FunctionCall node, NameResolutionLocallyVisibleVariables variables)
        {
            var declaration = GetDeclarationAndReportIfMissing(node.FunctionName, variables);
            if (declaration == null)
            {
                return new VisitorResult(variables);
            }
            return new VisitorResult(PartialAlgorithmResult.OfCalledFunction(node, (FunctionDefinition)declaration),
                variables);
        }


        public override VisitorResult VisitAssignment(Assignment node, NameResolutionLocallyVisibleVariables variables)
        {
            var declaration = GetDeclarationAndReportIfMissing(node.Left, variables);
            if (declaration == null)
            {
                return new VisitorResult(variables);
            }
            return new VisitorResult(PartialAlgorithmResult.OfAssignment(node, (VariableDeclaration)declaration),
                variables);
        }

        public override VisitorResult VisitVariableValue(VariableValue node, NameResolutionLocallyVisibleVariables variables)
        {
            var declaration = GetDeclarationAndReportIfMissing(node.Identifier, variables);
            if (declaration == null)
            {
                return new VisitorResult(variables);
            }
            return new VisitorResult(PartialAlgorithmResult.OfUsedVariable(node, declaration),
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
