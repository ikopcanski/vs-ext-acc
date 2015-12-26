using CodeContracts.Contrib.Managers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace CodeContracts.Contrib.Rewriters
{
    internal class ProxyClassDeclarationExtender : CSharpSyntaxRewriter
    {
        private string _newClassName;
        private string _implementedInterfaceName;

        public ProxyClassDeclarationExtender(string newClassName, string implementedInterfaceName)
        {
            _newClassName = newClassName;
            _implementedInterfaceName = implementedInterfaceName;
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            //Changing class declaration to "public sealed class <class-name>_proxy : <interface-name>

            var newClassName = SyntaxFactory.Identifier(_newClassName);

            //preparing access modifiers (oublic sealed)

            var modifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                                    SyntaxFactory.Token(SyntaxKind.SealedKeyword));

            //preparing interface name as base type

            var baseTypeSyntax = SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(_implementedInterfaceName));
            var baseTypes = SyntaxFactory.BaseList(new SeparatedSyntaxList<BaseTypeSyntax>().Add(baseTypeSyntax));

            //Creating and assembling new node.

            var extendedNode = node.WithIdentifier(newClassName)
                                    .WithModifiers(modifiers)
                                    .WithBaseList(baseTypes);

            //Formatting (white space, indents etc).

            return extendedNode.NormalizeWhitespace();
                                    
        }
    }
}
