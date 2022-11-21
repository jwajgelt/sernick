namespace sernick.Ast.Analysis.TypeChecking;

using System.Data;
using System.Xml.Linq;
using Diagnostics;
using NameResolution;
using Nodes;
using Utility;

using TypeInformation = Dictionary<Ast.Nodes.AstNode, Type>;

public sealed class TypeChecking
{
    public static TypeInformation CheckTypes(AstNode ast, NameResolutionResult nameResolution, IDiagnostics diagnostics)
    {
        // TODO: implement a TypeCheckingASTVisitor, walk it over the AST and initialize the property with the result
        var visitor = new TypeCheckingAstVisitor(nameResolution, diagnostics);
        return visitor.VisitAstTree(ast, Unit.I);
    }

    private class TypeCheckingAstVisitor : AstVisitor<TypeInformation, Unit>
    {
        /// <summary>
        /// Invariant: when visiting AstNode X, types for all children of X are known
        /// and stored in partialExpressionTypes dictionary
        /// </summary>
        private readonly NameResolutionResult nameResolution;
        private readonly IDiagnostics _diagnostics;
        /// <summary>
        /// Sometimes we would like to know the result for our ancestor,
        /// so to avoid recalculation (visiting the same ancestor from multiple nodes)
        /// we will have this helper object, containing type information for some AST nodes
        /// </summary>
        private readonly TypeInformation partialResult;
        /// <summary>
        /// Our Type-checking Algorighm is a top-down postorder
        /// But sometimes, nodes need to know information about some other nodes higher up the tree
        /// And in case we need to know a type information about our direct ancestor -- that's a circular dependency
        /// Which we have to uncover and report
        /// For example, a case like this:
        /// var x : Int = { 23; x = 23; } // here  a usage of x needs information about the type of x
        /// </summary>
        private readonly HashSet<AstNode> pendingNodes;

        public TypeCheckingAstVisitor(NameResolutionResult nameResolution, IDiagnostics diagnostics)
        {
            this.nameResolution = nameResolution;
            this._diagnostics = diagnostics;
            this.partialResult = new TypeInformation();
            this.pendingNodes = new HashSet<AstNode>();
        }

        protected override TypeInformation VisitAstNode(AstNode node, Unit _)
        {
            pendingNodes.Add(node);
            var result = node.Accept(this, Unit.I);
            pendingNodes.Remove(node);
            return result;
        }

        public override TypeInformation VisitIdentifier(Identifier identifierNode, Unit _)
        {
            // no need to visit children, identifier nodes should have none
            try
            {
                var variableDeclarationNode = this.getIdentifierAsVariableDeclaration(identifierNode);
                partialResult[identifierNode] = partialResult[variableDeclarationNode];
                return new TypeInformation() { { identifierNode, partialResult[variableDeclarationNode] } };
            }
            catch { }

            try
            {
                var functionDefinitionNode = this.getIdentifierAsFunctionDefinition(identifierNode);
                partialResult[identifierNode] = partialResult[functionDefinitionNode];
                return new TypeInformation() { { identifierNode, partialResult[functionDefinitionNode] } };
            }
            catch { }

            // An identifier should always point to either function definition or variable definition
            // So the code should never reach this line
            // But just in case, let's simply return unit here
            partialResult[identifierNode] = new UnitType();
            return new TypeInformation() { { identifierNode, new UnitType() } };


        }

        public override TypeInformation VisitVariableDeclaration(VariableDeclaration node, Unit _)
        {
            // all necessary checking should be performed in "Visit assignment", so just return Unit here
            pendingNodes.Add(node);
            var childrenTypes = this.visitNodeChildren(node);
            var result = new TypeInformation(childrenTypes);

            var declaredType = node.Type;
            if(declaredType != null && node.InitValue != null)
            {
                var rhsType = childrenTypes[node.InitValue];
                if(declaredType != rhsType)
                {
                    this._diagnostics.Report(new TypeCheckingError(declaredType, rhsType, node.LocationRange.Start));
                }
            }

            result.Add(node, new UnitType());
            pendingNodes.Remove(node);
            return result;

        }

        public override TypeInformation VisitFunctionParameterDeclaration(FunctionParameterDeclaration node, Unit _)
        {
            pendingNodes.Add(node);
            var childrenTypes = this.visitNodeChildren(node);

            var result = new TypeInformation(childrenTypes) { { node, new UnitType() } };
            pendingNodes.Remove(node);
            return result;
        }

        public override TypeInformation VisitFunctionDefinition(FunctionDefinition node, Unit _)
        {
            pendingNodes.Add(node);
            var childrenTypes = this.visitNodeChildren(node);

            var result = new TypeInformation(childrenTypes);
            result.Add(node, new UnitType());
            pendingNodes.Remove(node);
            return result;
        }

        public override TypeInformation VisitCodeBlock(CodeBlock node, Unit _)
        {
            pendingNodes.Add(node);
            var childrenTypes = this.visitNodeChildren(node);

            // simply return what expression inside returns?
            var result = new TypeInformation(childrenTypes);
            result.Add(node, childrenTypes[node.Inner]);
            pendingNodes.Remove(node);
            return result;

        }

        public override TypeInformation VisitExpressionJoin(ExpressionJoin node, Unit _)
        {
            pendingNodes.Add(node);
            var childrenTypes = this.visitNodeChildren(node);

            var result = new TypeInformation(childrenTypes);
            result.Add(node, childrenTypes[node.Second]); // just return the last expressions' type
            pendingNodes.Remove(node);
            return result;
        }

        public override TypeInformation VisitFunctionCall(FunctionCall functionCallNode, Unit _)
        {
            pendingNodes.Add(functionCallNode);
            var childrenTypes = this.visitNodeChildren(functionCallNode);

            var functionDeclarationNode = nameResolution.CalledFunctionDeclarations[functionCallNode];
            var declaredReturnType = functionDeclarationNode.ReturnType;

            
            var declaredArguments = functionDeclarationNode.Parameters;
            var actualArguments = functionCallNode.Arguments;

            if(declaredArguments.Count() != actualArguments.Count())
            {
                this._diagnostics.Report(new FunctionArgumentsMismatchError(declaredArguments.Count(), actualArguments.Count(), functionCallNode.LocationRange.Start));
            }

            declaredArguments.Zip<FunctionParameterDeclaration,Expression, Unit>(actualArguments, (declaredArgument, actualArgument) =>
            {
                // let us do type checking right here
                var expectedType = declaredArgument.Type;
                var actualType = childrenTypes[actualArgument];
                if (expectedType != actualType)
                {
                    this._diagnostics.Report(new WrongFunctionArgumentError(expectedType, actualType, functionCallNode.LocationRange.Start));
                }
                return Unit.I;
            });


            var result = new TypeInformation(childrenTypes);
            result.Add(functionCallNode, declaredReturnType);
            partialResult[functionCallNode] = declaredReturnType;
            pendingNodes.Remove(functionCallNode);
            return result;

        }

        public override TypeInformation VisitContinueStatement(ContinueStatement node, Unit _)
        {
            // TODO do we need to visit node.children here?
            pendingNodes.Add(node);
            var childrenTypes = this.visitNodeChildren(node);

            var result = new TypeInformation(childrenTypes);
            result.Add(node, new UnitType());
            pendingNodes.Remove(node);
            return result;
        }

        public override TypeInformation VisitReturnStatement(ReturnStatement node, Unit _)
        {
            pendingNodes.Add(node);
            var childrenTypes = this.visitNodeChildren(node);

            var result = new TypeInformation(childrenTypes);
            // Return Value is in a subtree, so its type should be already calculated by now
            if (node.ReturnValue != null)
            {
                result.Add(node, childrenTypes[node.ReturnValue] ?? new UnitType());
            }
            else
            {
                result.Add(node, new UnitType());
            }
            pendingNodes.Remove(node);
            return result;
        }

        public override TypeInformation VisitBreakStatement(BreakStatement node, Unit _)
        {
            // TODO do we need to visit node.children here?
            pendingNodes.Add(node);
            var childrenTypes = this.visitNodeChildren(node);

            var result = new TypeInformation(childrenTypes);
            result.Add(node, new UnitType());
            pendingNodes.Remove(node);
            return result;
        }


        public override TypeInformation VisitIfStatement(IfStatement node, Unit _)
        {
            pendingNodes.Add(node);
            var childrenTypes = this.visitNodeChildren(node);

            var typeOfTrueBranch = childrenTypes[node.IfBlock];
            if(node.ElseBlock != null)
            {
                var typeOfFalseBranch = childrenTypes[node.ElseBlock];
                if(typeOfTrueBranch != typeOfFalseBranch)
                {
                    this._diagnostics.Report(new TypeCheckingError(typeOfTrueBranch, typeOfFalseBranch, node.LocationRange.Start));
                }
            }

            var result = new TypeInformation(childrenTypes);
            result.Add(node, typeOfTrueBranch);
            partialResult[node] = typeOfTrueBranch;
            pendingNodes.Remove(node);
            return result;
        }

        public override TypeInformation VisitLoopStatement(LoopStatement node, Unit _)
        {
            pendingNodes.Add(node);
            var childrenTypes = this.visitNodeChildren(node);

            // TODO what if Loop contains a "return 12;"?
            var result = new TypeInformation(childrenTypes);
            result.Add(node, new UnitType());
            pendingNodes.Remove(node);
            return result;
        }


        public override TypeInformation VisitInfix(Infix node, Unit _)
        {
            pendingNodes.Add(node);
            var childrenTypes = this.visitNodeChildren(node);

            var typeOfLeftOperand = childrenTypes[node.LeftSide];
            var typeOfRightOperand = childrenTypes[node.RightSide];

            if(typeOfLeftOperand == new UnitType() || typeOfRightOperand == new UnitType())
            {
                this._diagnostics.Report(new UnitTypeInfixOperatorError(node.LocationRange.Start));
                var result = new TypeInformation(childrenTypes);
                result.Add(node, new UnitType());
                pendingNodes.Remove(node);
                return result;
            }

            if (typeOfLeftOperand.ToString() != typeOfRightOperand.ToString())
            {
                // TODO we probably should create a separate error class for operator type mismatch
                this._diagnostics.Report(new TypeCheckingError(typeOfLeftOperand, typeOfRightOperand, node.LocationRange.Start));

                // TODO does it make sense to return anything here? maybe a Unit type? But it could propagate the error up the tree 
                var result = new TypeInformation(childrenTypes);
                result.Add(node, new UnitType());
                pendingNodes.Remove(node);
                return result;
            }
            else
            {
                var commonType = typeOfLeftOperand;

                // let's cover some special cases e.g. adding two bools or shirt-curcuiting two ints
                switch (node.Operator)
                {
                    case Infix.Op.Plus:
                    case Infix.Op.Minus:
                        {
                        if(commonType.ToString() == new BoolType().ToString())
                            {
                                this._diagnostics.Report(new TypeCheckingError(typeOfLeftOperand, typeOfRightOperand, node.LocationRange.Start));
                            }
                            break;
                        }
                    case Infix.Op.Greater:
                    case Infix.Op.GreaterOrEquals:
                    case Infix.Op.Less:
                    case Infix.Op.LessOrEquals:
                        {
                            if(commonType.ToString() != new IntType().ToString())
                            {
                                this._diagnostics.Report(new TypeCheckingError(typeOfLeftOperand, typeOfRightOperand, node.LocationRange.Start));
                            }
                            break;
                        }
                    case Infix.Op.ScAnd:
                    case Infix.Op.ScOr:
                        {
                            if(commonType.ToString() != new BoolType().ToString())
                            {
                                this._diagnostics.Report(new TypeCheckingError(typeOfLeftOperand, typeOfRightOperand, node.LocationRange.Start));
                            }
                            break;
                        }
                    default:
                        {
                            break;
                        }      
                }
                var result = new TypeInformation(childrenTypes);
                result.Add(node, commonType);
                pendingNodes.Remove(node);
                return result;
            }
        }

        public override TypeInformation VisitAssignment(Assignment node, Unit _)
        {
            pendingNodes.Add(node);
            var childrenTypes = this.visitNodeChildren(node);

            var typeOfLeftSide = childrenTypes[node.Left];
            var typeOfRightSide = childrenTypes[node.Right];
            if (typeOfLeftSide.ToString() != typeOfRightSide.ToString())
            {
                this._diagnostics.Report(new TypeCheckingError(typeOfLeftSide, typeOfRightSide, node.LocationRange.Start));
            }

            // Regardless of the error, let's return a Unit type for assignment and get more type checking information
            var result = new TypeInformation(childrenTypes);
            result.Add(node, new UnitType());
            pendingNodes.Remove(node);
            return result;
        }

        public override TypeInformation VisitVariableValue(VariableValue node, Unit _)
        {
            pendingNodes.Add(node);
            var childrenTypes = this.visitNodeChildren(node);

            var result = new TypeInformation(childrenTypes);

            var variableIdentifier = node.Identifier;

            result.Add(node, result[node.Identifier]);
            pendingNodes.Remove(node);
            return result;
        }

        public override TypeInformation VisitBoolLiteralValue(BoolLiteralValue node, Unit _)
        {
            // No need to visit node children here

            var result = new TypeInformation() { { node, new BoolType() } };
            return result;
        }

        public override TypeInformation VisitIntLiteralValue(IntLiteralValue node, Unit _)
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
            var nodeHasNoChildren = !node.Children.Any();
            if(nodeHasNoChildren)
            {
                return new TypeInformation();
            }

            var avoidRecalculation = partialResult.ContainsKey(node);
            if (avoidRecalculation)
            {
                return partialResult;
            }

            return node.Children.Aggregate(new TypeInformation(),
                (partialTypeInformation, childNode) =>
                {
                    var resultForChildNode = childNode.Accept(this, Unit.I);
                    return (new List<TypeInformation> { partialTypeInformation, resultForChildNode }).SelectMany(dict => dict)
                         .ToDictionary(pair => pair.Key, pair => pair.Value);
                }
           );
        }

        private VariableDeclaration getIdentifierAsVariableDeclaration(Identifier identifier)
        {
            foreach(var declarationPair in nameResolution.AssignedVariableDeclarations)
            {
                var assignment = declarationPair.Key;
                var variableDeclaration = declarationPair.Value;

                if(variableDeclaration.Name == identifier)
                {
                    return variableDeclaration;
                }
            }
            throw new Exception();
        }

        private FunctionDefinition getIdentifierAsFunctionDefinition(Identifier identifier)
        {
            foreach (var declarationPair in nameResolution.CalledFunctionDeclarations)
            {
                var assignment = declarationPair.Key;
                var functionDefinition = declarationPair.Value;

                if (functionDefinition.Name == identifier)
                {
                    return functionDefinition;
                }
            }
            throw new Exception();
        }

    }


    
}
