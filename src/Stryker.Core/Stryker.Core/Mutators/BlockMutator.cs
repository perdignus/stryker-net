using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using Stryker.Core.Mutants;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Stryker.Core.Mutators
{
    public class BlockMutator : Mutator<BlockSyntax>, IMutator
    {
        public override IEnumerable<Mutation> ApplyMutations(BlockSyntax node)
        {

            if (node.Kind() == SyntaxKind.Block)
            {
                BlockSyntax replacementNode = SyntaxFactory.Block();
                if (node.Parent.Kind() == SyntaxKind.MethodDeclaration)
                {
                    var returnType = GetReturnType(node.Parent);
                    var isReturnTypeVoid = returnType is PredefinedTypeSyntax predefinedReturnType && predefinedReturnType.Keyword.Text == "void";
                    if (!isReturnTypeVoid)
                    {
                        var defaultReturnType = SyntaxFactory.DefaultExpression(returnType);
                        var returnStatement = SyntaxFactory.ReturnStatement(defaultReturnType);
                        replacementNode = SyntaxFactory.Block(returnStatement);
                    }
                }

                yield return new Mutation()
                {
                    OriginalNode = node,
                    ReplacementNode = replacementNode,
                    DisplayName = "Remove code block",
                    Type = MutatorType.Block
                };
            }
        }

        private TypeSyntax GetReturnType(SyntaxNode methodDeclerationNode)
        {
            var returnType = methodDeclerationNode.ChildNodes().FirstOrDefault(childNode => childNode is TypeSyntax foundReturnType);
            return returnType as TypeSyntax;
        }
    }
}
