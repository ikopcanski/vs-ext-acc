using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace CodeContracts.Contrib.Rewriters
{
    internal class CCInterfaceImplementor : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            //Replacing interface property declaration with default property implementation (getters that return default value of return type or empty body)

            var returnType = node.Type;
            var identifier = node.Identifier;

            //Determining whether we have geter and/or setter.

            var hasGetter = false;
            var hasSetter = false;
            var accessorsList = node.ChildNodes().FirstOrDefault(cn => cn.Kind() == SyntaxKind.AccessorList);
            if (accessorsList != null)
            {
                hasGetter = accessorsList.ChildNodes().Any(cn => cn.Kind() == SyntaxKind.GetAccessorDeclaration);
                hasSetter = accessorsList.ChildNodes().Any(cn => cn.Kind() == SyntaxKind.SetAccessorDeclaration);
            }

            //Creating accessors list node.

            var accessorsListNode = SyntaxFactory.AccessorList(new SyntaxList<AccessorDeclarationSyntax>());

            if (hasGetter)
            {
                var returnStatement = string.Format("\r\n//Code contract checks here...\r\nreturn default({0});\r\n", returnType);
                var getterBody = SyntaxFactory.Block(SyntaxFactory.ParseStatement(returnStatement));
                var getterNode = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, getterBody);
                accessorsListNode = accessorsListNode.AddAccessors(getterNode);
            }

            if (hasSetter)
            {
                var setterBody = GetEmptyBlockWithComment();
                var setterNode = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration, setterBody);
                accessorsListNode = accessorsListNode.AddAccessors(setterNode);
            }

            //Assembling and returning property node.

            var publicModifier = SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            var propertyNode = SyntaxFactory.PropertyDeclaration(returnType, identifier).WithModifiers(publicModifier).WithAccessorList(accessorsListNode);

            return propertyNode.NormalizeWhitespace();
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            //Replacing interface method declaration with default method implementation (empty body method or with default return statement).

            var returnType = node.ReturnType;
            var identifier = node.Identifier;
            var parametersList = node.ParameterList;

            //if return value of method is not void, we have a return statement.


            var bodyDeclaration = GetEmptyBlockWithComment();
            var returnTypeValue = returnType.ToFullString().Trim();
            if (returnTypeValue != "void")
            {
                var returnStatement = string.Format("\r\n//Code contract checks here...\r\nreturn default({0});\r\n", returnTypeValue);
                bodyDeclaration = SyntaxFactory.Block(SyntaxFactory.ParseStatement(returnStatement));

            }

            //Assembling and returning method node.

            var publicModifier = SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            var fullMethodNode = node.WithModifiers(publicModifier).WithBody(bodyDeclaration);

            //Removing trailing trivia - semicolon

            var semicolonToken = fullMethodNode.GetLastToken();
            var noneToken = SyntaxFactory.Token(SyntaxKind.None);
            fullMethodNode = fullMethodNode.ReplaceToken(semicolonToken, noneToken);

            return fullMethodNode.NormalizeWhitespace();
        }

        public override SyntaxNode VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
        {
            //Adding public keyword, since this will be a class declaration, not interface.

            return node.WithModifiers(new SyntaxTokenList().Add(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));
        }

        private BlockSyntax GetEmptyBlockWithComment()
        {
            var blockNode = SyntaxFactory.Block(SyntaxFactory.ParseStatement(";\t//Code contract checks here..."));
            var semicolon = blockNode.ChildNodes().FirstOrDefault(ct => ct.Kind() == SyntaxKind.EmptyStatement);
            blockNode = blockNode.RemoveNode(semicolon, SyntaxRemoveOptions.KeepLeadingTrivia | SyntaxRemoveOptions.KeepTrailingTrivia);
            return blockNode;
        }
    }
}
