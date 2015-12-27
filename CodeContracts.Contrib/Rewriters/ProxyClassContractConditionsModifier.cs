using CodeContracts.Contrib.Helpers;
using CodeContracts.Contrib.Managers;
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
            //Transforming Contract.Requires<>(precondition, message) and Contract.Ensures(postcondition, message) statements into block like this:
            //if (precondition) throw Exception(message); 
            //var retVal = _contract.Method(params);
            //if (postcondition) throw Exception(message);
            //return retVal;

            bool isVoid = node.ReturnType.Str().Trim() == "void";

            var methodName = node.Identifier.Str();
            var parametersList = node.FirstChild<ParameterListSyntax>();
            string argumentsStr = string.Join(", ", parametersList.Parameters.Select(p => p.Identifier.Str()).ToArray());

            StatementSyntax contractCallStatement = SyntaxFactory.ParseStatement(string.Format("{0}.{1}({2});\r\n", IdentifiersHelper.ProxyContractFieldName, methodName, argumentsStr));
            StatementSyntax returnStatement = null;

            if (!isVoid)
            {
                
                var contractCallExpressionStr = string.Format("var {0} = {1}.{2}({3});\r\n", IdentifiersHelper.ProxyContractRetVal, IdentifiersHelper.ProxyContractFieldName, methodName, argumentsStr);
                contractCallStatement = SyntaxFactory.ParseStatement(contractCallExpressionStr);
                returnStatement = SyntaxFactory.ParseStatement(string.Format("return {0};", IdentifiersHelper.ProxyContractRetVal));
            }
            
            var expressions = node.ChildrenOfType<ExpressionStatementSyntax>();
            var statements = new ContractExpressionTransformer(contractCallStatement, returnStatement).Transform(expressions);
            return node.WithBody(SyntaxFactory.Block(statements));
        }
    }
}
