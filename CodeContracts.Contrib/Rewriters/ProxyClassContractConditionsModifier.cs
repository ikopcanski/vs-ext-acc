using CodeContracts.Contrib.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace CodeContracts.Contrib.Rewriters
{
    public class ProxyClassContractConditionsModifier : CSharpSyntaxRewriter
    {
        private string _contractFieldName;

        public ProxyClassContractConditionsModifier(string contractFieldName)
        {
            _contractFieldName = contractFieldName;
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            bool isVoid = node.ReturnType.ToFullString().Trim() == "void";

            var requireStatements = node.ChildrenOfType<ExpressionStatementSyntax>().Where(e => e.ToFullString().Contains("Contract.Requires<"));
            
            var ensureStatements = node.ChildrenOfType<ExpressionStatementSyntax>().Where(e => e.ToFullString().Contains("Contract.Ensures<"));

            return node;
        }
    }
}
