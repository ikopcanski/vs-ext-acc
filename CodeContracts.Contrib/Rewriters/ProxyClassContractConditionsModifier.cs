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
            //Transforming Contract.Requires<>(precondition, message) and Contract.Ensures(postcondition, message) statements into block like this:
            //if (precondition) throw Exception(message); 
            //var retVal = _contract.Method(params);
            //if (postcondition) throw Exception(message);
            //return retVal;

            bool isVoid = node.ReturnType.Str().Trim() == "void";

            StatementSyntax returnStatement = SyntaxFactory.EmptyStatement();
            if (isVoid)
            {
                returnStatement = SyntaxFactory.ReturnStatement(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                                                SyntaxFactory.ParseExpression("int i = 1; //TODO: _contract(<params>)"),
                                                                SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            }
            
            var expressions = node.ChildrenOfType<ExpressionStatementSyntax>();
            var statements = new ContractExpressionTransformer(returnStatement).Transform(expressions);
            return node.WithBody(SyntaxFactory.Block(statements));
        }
    }
}
