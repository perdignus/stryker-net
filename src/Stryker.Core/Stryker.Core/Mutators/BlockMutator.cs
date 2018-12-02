using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using Stryker.Core.Mutants;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using Microsoft.CodeAnalysis;
using System;
using Microsoft.Extensions.Logging;
using Stryker.Core.Logging;

namespace Stryker.Core.Mutators
{
    public class BlockMutator : Mutator<BlockSyntax>, IMutator
    {
        private ILogger _logger { get; set; }

        public BlockMutator()
        {
            _logger = ApplicationLogging.LoggerFactory.CreateLogger<BlockMutator>();
        }

        public override IEnumerable<Mutation> ApplyMutations(BlockSyntax node)
        {
            List<Mutation> mutations = new List<Mutation>();
            try
            {
                var blockMutation = CreateBlockMutation(node);
                if (blockMutation != null)
                {
                    mutations.Add(blockMutation);
                }
            }
            catch (ReturnTypeNotFoundException exception)
            {
                _logger.LogDebug(exception.Message);
            }

            return mutations;
        }

        private Mutation CreateBlockMutation(BlockSyntax node)
        {
            Mutation createdBlockMutation = null;
            if (node.Kind() == SyntaxKind.Block)
            {
                BlockSyntax replacementNode = CreateBlockWithCorrectReturnNode(node.Parent);

                createdBlockMutation = new Mutation()
                {
                    OriginalNode = node,
                    ReplacementNode = replacementNode,
                    DisplayName = "Remove code block",
                    Type = MutatorType.Block
                };
            }

            return createdBlockMutation;
        }

        private BlockSyntax CreateBlockWithCorrectReturnNode(SyntaxNode parentNode)
        {
            BlockSyntax replacementNode = SyntaxFactory.Block();
            var parentType = parentNode.Kind();
            bool isParentOfTypeMethod = parentType == SyntaxKind.MethodDeclaration;
            bool isParentOfTypeGetProperty = parentType == SyntaxKind.GetAccessorDeclaration;
            bool isParentOfTypeLambda = parentType == SyntaxKind.SimpleLambdaExpression || parentType == SyntaxKind.ParenthesizedLambdaExpression;

            if (isParentOfTypeMethod || isParentOfTypeGetProperty || isParentOfTypeLambda)
            {
                var nodeWithReturnType = parentNode;
                if (isParentOfTypeGetProperty)
                {
                    nodeWithReturnType = parentNode.Parent.Parent;
                }
                else if (isParentOfTypeLambda)
                {
                    nodeWithReturnType = GetParentNodeForLambda(parentNode);
                }

                TypeSyntax returnType = GetReturnType(nodeWithReturnType);

                var isReturnTypeVoid = returnType is PredefinedTypeSyntax predefinedReturnType && predefinedReturnType.Keyword.Text == "void";
                if (!isReturnTypeVoid)
                {
                    var returnStatement = CreateReturnDefaultStatement(returnType);
                    replacementNode = SyntaxFactory.Block(returnStatement);
                }


            }
            return replacementNode;
        }

        private TypeSyntax GetReturnType(SyntaxNode methodDeclerationNode)
        {
            var returnType = methodDeclerationNode.ChildNodes().LastOrDefault(childNode => childNode is TypeSyntax || childNode is PredefinedTypeSyntax);
            return returnType as TypeSyntax;
        }

        private SyntaxNode GetParentNodeForLambda(SyntaxNode parentNode)
        {
            SyntaxNode nodeWithReturnType;
            var lambdaDeclerationNode = parentNode.Parent.Parent.Parent;
            if (lambdaDeclerationNode.Kind() != SyntaxKind.VariableDeclaration)
            {
                throw new ReturnTypeNotFoundException("Lambda without inline type declaration not yet supported");
            }

            var lambdaNodeName = GetFirstChildOfType<GenericNameSyntax>(lambdaDeclerationNode);
            nodeWithReturnType = GetFirstChildOfType<TypeArgumentListSyntax>(lambdaNodeName);

            return nodeWithReturnType;
        }

        private SyntaxNode GetFirstChildOfType<T>(SyntaxNode node)
        {
            return node.ChildNodes().FirstOrDefault(childNode => childNode is T);
        }

        private ReturnStatementSyntax CreateReturnDefaultStatement(TypeSyntax returnType)
        {
            var defaultReturnType = SyntaxFactory.DefaultExpression(returnType);

            var returnKeyword = SyntaxFactory.Token(SyntaxKind.ReturnKeyword);
            returnKeyword = returnKeyword.WithTrailingTrivia(SyntaxFactory.Space);
            var semicolon = SyntaxFactory.Token(SyntaxKind.SemicolonToken);

            var returnStatement = SyntaxFactory.ReturnStatement(returnKeyword, defaultReturnType, semicolon); // Could also be done with just "defaultReturnType", but it would show as "returndefault" instead of "return default"
            return returnStatement;
        }

        private class ReturnTypeNotFoundException : NotImplementedException
        {
            public ReturnTypeNotFoundException(string message) : base(message) { }
        }
    }
}
