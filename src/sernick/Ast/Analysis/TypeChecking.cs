namespace sernick.Ast.Analysis;

using System.Data;
using System.Xml.Linq;
using Diagnostics;
using NameResolution;
using Nodes;
using Utility;

using TypeInformation = Dictionary<Ast.Nodes.AstNode, Type>;

public sealed class TypeChecking
{
    /// <summary>
    /// Maps expressions to their types
    /// </summary>
    public IReadOnlyDictionary<Expression, Type> ExpressionTypes
    {
        get;
        init;
    }

    public static TypeInformation CheckTypes(AstNode ast, NameResolutionResult nameResolution, Diagnostics diagnostics)
    {
        // TODO: implement a TypeCheckingASTVisitor, walk it over the AST and initialize the property with the result
        var visitor = new TypeCheckingAstVisitor(nameResolution, diagnostics);
        return visitor.VisitAstTree(ast, new TypeInformation());
    }

    private class TypeCheckingAstVisitor : AstVisitor<TypeInformation, Dictionary<AstNode, Type>>
    {
        /// <summary>
        /// Invariant: when visiting AstNode X, types for all children of X are known
        /// and stored in partialExpressionTypes dictionary
        /// </summary>
        private IReadOnlyDictionary<AstNode, Type> partialExpressionsTypess;
        private readonly NameResolutionResult nameResolution;
        private Diagnostics _diagnostics;

        public TypeCheckingAstVisitor(NameResolutionResult nameResolution, Diagnostics diagnostics)
        {
            this.nameResolution = nameResolution;
            this._diagnostics = diagnostics;
        }

        protected override TypeInformation VisitAstNode(AstNode node, TypeInformation _)
        {
            // just visit recursively, bottom-up
            // for simple things, just visit them without recursion
            if(node.Children.Count() == 0)
            {
                var emptyTypeInformation = new TypeInformation();
                return node.Accept(this, emptyTypeInformation);
            }

            var childrenTypes = this.visitNodeChildren(node);

            // TODO what to do after we've visited node children


            return childrenTypes;
        }

        public override TypeInformation VisitVariableDeclaration(VariableDeclaration node, TypeInformation _)
        {
            // all necessary checking should be performed in "Visit assignment", so just return Unit here
            var childrenTypes = this.visitNodeChildren(node);
            var result = new TypeInformation(childrenTypes);
            result.Add(node, new UnitType());
            return result;

        }

        public override TypeInformation VisitFunctionParameterDeclaration(FunctionParameterDeclaration node, TypeInformation _)
        {
            var childrenTypes = this.visitNodeChildren(node);

            var result = new TypeInformation(childrenTypes);
            result.Add(node, new UnitType());
            return result;
        }

        public override TypeInformation VisitFunctionDefinition(FunctionDefinition node, TypeInformation _)
        {
            var childrenTypes = this.visitNodeChildren(node);

            var result = new TypeInformation(childrenTypes);
            result.Add(node, new UnitType());
            return result;
        }

        public override TypeInformation VisitCodeBlock(CodeBlock node, TypeInformation _)
        {
            var childrenTypes = this.visitNodeChildren(node);

            // simply return what expression inside returns?
            var result = new TypeInformation(childrenTypes);
            result.Add(node, childrenTypes[node.Inner]);
            return result;

        }

        public override TypeInformation VisitExpressionJoin(ExpressionJoin node, TypeInformation _)
        {
            var childrenTypes = this.visitNodeChildren(node);

            var result = new TypeInformation(childrenTypes);
            result.Add(node, childrenTypes[node.Second]); // just return the last expressions' type
            return result;
        }

        public override TypeInformation VisitFunctionCall(FunctionCall functionCallNode, TypeInformation _)
        {
            var childrenTypes = this.visitNodeChildren(functionCallNode);

            var functionDeclarationNode = nameResolution.CalledFunctionDeclarations[functionCallNode];
            var declaredReturnType = functionDeclarationNode.ReturnType;

            var inferredReturnType = new UnitType(); // TODO maybe just add return type from function declaration?

            if(inferredReturnType.ToString() != declaredReturnType.ToString())
            {
                this._diagnostics.Report(new TypeCheckingError(declaredReturnType, inferredReturnType, functionCallNode.LocationRange.Start));
                return childrenTypes; // not sure here
            }

            var result = new TypeInformation(childrenTypes);
            result.Add(functionCallNode, new UnitType()); // function call returns a unit, not a function return type
            return result;

        }

        public override TypeInformation VisitContinueStatement(ContinueStatement node, TypeInformation _)
        {
            // TODO do we need to visit node.children here?
            var childrenTypes = this.visitNodeChildren(node);

            var result = new TypeInformation(childrenTypes);
            result.Add(node, new UnitType());
            return result;
        }

        public override TypeInformation VisitReturnStatement(ReturnStatement node, TypeInformation _)
        {
            var childrenTypes = this.visitNodeChildren(node);

            var result = new TypeInformation(childrenTypes);
            // Return Value is in a subtree, so its type should be already calculated by now
            result.Add(node, childrenTypes[node.ReturnValue]);
            return result;
        }

        public override TypeInformation VisitBreakStatement(BreakStatement node, TypeInformation _)
        {
            // TODO do we need to visit node.children here?
            var childrenTypes = this.visitNodeChildren(node);

            var result = new TypeInformation(childrenTypes);
            result.Add(node, new UnitType());
            return result;
        }


        public override TypeInformation VisitIfStatement(IfStatement node, TypeInformation param)
        {
            param = this.visitNodeChildren(node);

            // TODO
            return null;
        }

        public override TypeInformation VisitLoopStatement(LoopStatement node, TypeInformation partialExpressionTypes)
        {
            partialExpressionTypes = this.visitNodeChildren(node);

            // TODO what if Loop contains a "return 12;"?
            var result = new TypeInformation(partialExpressionTypes);
            result.Add(node, new UnitType());
            return result;
        }


        public override TypeInformation VisitInfix(Infix node, TypeInformation _)
        {
            var childrenTypes = this.visitNodeChildren(node);

            var typeOfLeftOperand = childrenTypes[node.LeftSide];
            var typeOfRightOperand = childrenTypes[node.RightSide];

            if(typeOfLeftOperand.ToString() != typeOfRightOperand.ToString())
            {
                // TODO we probably should create a separate error class for operator type mismatch
                this._diagnostics.Report(new TypeCheckingError(typeOfLeftOperand, typeOfRightOperand, node.LocationRange.Start));

                // TODO does it make sense to return anything here? maybe a Unit type? But it could propagate the error up the tree 
                var result = new TypeInformation(childrenTypes);
                result.Add(node, new UnitType());
                return result;
            }
            else
            {
                var commonType = typeOfLeftOperand;
                var result = new TypeInformation(childrenTypes);
                result.Add(node, commonType);
                return result;
            }
        }

        public override TypeInformation VisitAssignment(Assignment node, TypeInformation _)
        {
            var childrenTypes = this.visitNodeChildren(node);

            var typeOfLeftSide = childrenTypes[node.LeftSide];
            var typeOfRightSide = childrenTypes[node.RightSide];
            if (typeOfLeftSide.ToString() != typeOfLeftSide.ToString())
            {
                this._diagnostics.Report(new TypeCheckingError(typeOfLeftSide, typeOfRightSide, node.LocationRange.Start));
            }

            // Regardless of the error, let's return a Unit type for assignment and get more type checking information
            var result = new TypeInformation(childrenTypes);
            result.Add(node, new UnitType());
            return result;
        }

        public override TypeInformation VisitVariableValue(VariableValue node, TypeInformation _)
        {
            var childrenTypes = this.visitNodeChildren(node);

            var result = new TypeInformation(childrenTypes);

            var variableDeclarationNode = this.nameResolution.UsedVariableDeclarations[node];
            var variableHasExplicitType = variableDeclarationNode.Type != null;

            // explicit type near variable declaration
            if(variableDeclarationNode.Type != null)
            {
                result.Add(node, variableDeclarationNode.Type);
                return result;
            }

            // if there is no type, variable must have been immediately initialized (language-wide decision)
            // but we should check it for not being equal null just so C# compiler is satisfied
            if (variableDeclarationNode.InitValue != null)
            {
                var typeInformation2 = VisitExpression(variableDeclarationNode.InitValue, childrenTypes);
                var typeOfExpression = typeInformation2[variableDeclarationNode.InitValue];

                result.Add(node, typeOfExpression);
                return result;
            }

            // this "return" should never be reached
            return result;
        }


        public override TypeInformation VisitBoolLiteralValue(BoolLiteralValue node, TypeInformation partialExpressiontypes)
        {
            // No need to visit node children here

            var result = new TypeInformation(){ { node, new BoolType() } };
            return result;
        }

        public override TypeInformation VisitIntLiteralValue(IntLiteralValue node, TypeInformation partialExpressiontypes)
        {
            // No need to visit node children here

            var result = new TypeInformation() { { node, new IntType() } };
            return result;
        }

        /// <summary>
        /// Since we want to do a bottom-up recursion, but we're calling our node.Accept functions
        /// in a top-down order, before actually processing a node we have to make sure we've visited all of its
        /// children. This helper method should be thus called at the beginning of almost each "Visit" function,
        /// except simple AST nodes (e.g. int literal), where we know there would be no recursion
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private TypeInformation visitNodeChildren(AstNode node)
        {
            return node.Children.Aggregate(new TypeInformation(),
                (partialTypeInformation, childNode) =>
                {
                    var resultForChildNode = childNode.Accept(this, new TypeInformation());
                    return (new List<TypeInformation> { partialTypeInformation, resultForChildNode}).SelectMany(dict => dict)
                         .ToDictionary(pair => pair.Key, pair => pair.Value);
                }
           );
        }

    }
}