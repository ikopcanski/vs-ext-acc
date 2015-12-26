using CodeContracts.Contrib.Managers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace CodeContracts.Contrib.Rewriters
{
    internal class CCInterfaceDeclarationExtender : CSharpSyntaxRewriter
    {
        private string _newInterfaceName;

        public CCInterfaceDeclarationExtender(string newInterfaceName)
        {
            _newInterfaceName = newInterfaceName;
        }

        public override SyntaxNode VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            //Changing interface declaration to "internal abstract sealed interface <interface-name>_cc : <interface-name>

            var newClassName = SyntaxFactory.Identifier(_newInterfaceName);

            //preparing access modifiers (internal abstract sealed)

            var modifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.InternalKeyword),
                                                    SyntaxFactory.Token(SyntaxKind.AbstractKeyword));

            //preparing interface name as base type

            var baseTypeSyntax = SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(node.Identifier.Text.Trim()));
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
