using CodeContracts.Contrib.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;
using System.Linq;

namespace CodeContracts.Contrib.Rewriters
{
    internal class ProxyClassConstructorCreator : CSharpSyntaxRewriter
    {
        private string _interfaceName;

        public ProxyClassConstructorCreator(string interfaceName)
        {
            _interfaceName = interfaceName;
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var declarators = SyntaxFactory.SeparatedList<VariableDeclaratorSyntax>().Add(SyntaxFactory.VariableDeclarator(IdentifiersHelper.ProxyContractFieldName));
            var variable = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(_interfaceName), declarators);
            var field = SyntaxFactory.FieldDeclaration(variable)
                                     .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                                                                            SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))); 

            var parameters = SyntaxFactory.ParseParameterList(string.Format("({0} {1})", _interfaceName, IdentifiersHelper.ProxyContractParameterName));
            var statement = SyntaxFactory.ParseStatement(string.Format("{0} = {1};", IdentifiersHelper.ProxyContractFieldName, IdentifiersHelper.ProxyContractParameterName));
            var block = SyntaxFactory.Block().WithStatements(SyntaxFactory.List(new[] { statement }));
            var constructor = SyntaxFactory.ConstructorDeclaration(node.Identifier)
                                           .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                                           .WithParameterList(parameters)
                                           .WithBody(block);

            var firstMethod = node.ChildrenOfType<MethodDeclarationSyntax>().FirstOrDefault();
            return node.InsertNodesBefore(firstMethod, new SyntaxNode[] { field, constructor });
        }
    }
}
