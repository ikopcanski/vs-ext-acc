using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using CodeContracts.Contrib.Helpers;

namespace CodeContracts.Contrib.Rewriters
{
    internal class ClassDeclarationAttributeExtender : CSharpSyntaxRewriter
    {
        private string _attributeName;
        private string _argumentTypeName;
        private bool _deleteExistingAttributes;


        public ClassDeclarationAttributeExtender(string attributeName, string argumentTypeName, bool deleteExistingAttributes)
        {
            _attributeName = attributeName;
            _argumentTypeName = argumentTypeName;
            _deleteExistingAttributes = deleteExistingAttributes;
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (ContainsAttribute(node))
            {
                return node;
            }

            if (_deleteExistingAttributes)
            {
                var attributes = GetSyntaxList();
                return node.WithAttributeLists(attributes);
            }
            else
            {
                var attributes = GetAttributesList();
                return node.AddAttributeLists(attributes);
            }
            
        }

        private SyntaxList<AttributeListSyntax> GetSyntaxList()
        {
            var typeOfExpression = string.Format("typeof({0})", _argumentTypeName ?? "");
            var attributeArg = SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression(typeOfExpression));
            var attributeArgsList = SyntaxFactory.AttributeArgumentList(new SeparatedSyntaxList<AttributeArgumentSyntax>().Add(attributeArg));
            var attribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName(_attributeName), attributeArgsList);
            var list = SyntaxFactory.AttributeList(new SeparatedSyntaxList<AttributeSyntax>().Add(attribute));
            return new SyntaxList<AttributeListSyntax>().Add(list);
        }

        private AttributeListSyntax GetAttributesList()
        {
            var typeOfExpression = string.Format("typeof({0})", _argumentTypeName ?? "");
            var attributeArg = SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression(typeOfExpression));
            var attributeArgsList = SyntaxFactory.AttributeArgumentList(new SeparatedSyntaxList<AttributeArgumentSyntax>().Add(attributeArg));
            var attribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName(_attributeName), attributeArgsList);
            return SyntaxFactory.AttributeList(new SeparatedSyntaxList<AttributeSyntax>().Add(attribute));
        }

        private bool ContainsAttribute(SyntaxNode node)
        {
            return node.ChildrenOfType<AttributeSyntax>().Any(a => ((IdentifierNameSyntax)a.Name).Identifier.Text.Trim() == _attributeName);
        }
    }
}
