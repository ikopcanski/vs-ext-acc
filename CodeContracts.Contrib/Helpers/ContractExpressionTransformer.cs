using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeContracts.Contrib.Helpers
{
    internal class ContractExpressionTransformer
    {
        private List<IContractExpressionStrategy> _strategies;
        private StatementSyntax _returnStatement; 

        public ContractExpressionTransformer(StatementSyntax returnStatement)
        {
            _strategies = new List<IContractExpressionStrategy>()
            {
                new GenericRequiresExpressionStrategy(),
                new SimpleRequiresExpressionStrategy(),
                new EnsuresExpressionStrategy(),
                new EmptyExpressionStrategy()
            }
            .OrderByDescending(s => s.Priority).ToList();
            _returnStatement = returnStatement;
        }

        private StatementSyntax Transform(ExpressionStatementSyntax exp)
        {
            StatementSyntax retVal = null;

            foreach (var strategy in _strategies)
            {
                if (strategy.IsMine(exp))
                {
                    retVal = strategy.Transform(exp);
                    break;
                }
            }

            return retVal;
        }

        public IEnumerable<StatementSyntax> Transform(IEnumerable<ExpressionStatementSyntax> expressions)
        {
            foreach (var expression in expressions)
            {
                yield return Transform(expression);
            }
        }
    }

    internal interface IContractExpressionStrategy
    {
        int Priority { get; }
        bool IsMine(ExpressionStatementSyntax exp);
        StatementSyntax Transform(ExpressionStatementSyntax exp);
    }

    internal class GenericRequiresExpressionStrategy : IContractExpressionStrategy
    {
        public int Priority { get { return 100; } }

        public bool IsMine(ExpressionStatementSyntax exp)
        {
            return exp.Str().StartsWith("Contract.Requires<");
        }

        public StatementSyntax Transform(ExpressionStatementSyntax exp)
        {
            var exceptionType = exp.FirstChild<GenericNameSyntax>()
                                   .ChildrenOfType<IdentifierNameSyntax>()
                                   .FirstOrDefault().Str();

            var argumentsList = exp.FirstChild<ArgumentListSyntax>();

            var condition = argumentsList.Arguments.FirstOrDefault().Str();

            var message = string.Format("Precondition '{0}' is not satisfied!", condition);
            
            if (argumentsList.Arguments.Count == 2)
            {
                message = argumentsList.Arguments.Skip(1).FirstOrDefault().Str();
            }

            var statementStr = string.Format("if ({0}) {{ throw new {1}({2}); }}", condition, exceptionType, message);

            return SyntaxFactory.ParseStatement(statementStr);
        }
    }

    internal class SimpleRequiresExpressionStrategy : IContractExpressionStrategy
    {
        public int Priority { get { return 80; } }
        
        public bool IsMine(ExpressionStatementSyntax exp)
        {
            return exp.Str().StartsWith("Contract.Requires(");
        }

        public StatementSyntax Transform(ExpressionStatementSyntax exp)
        {
            var argumentsList = exp.FirstChild<ArgumentListSyntax>();

            var condition = argumentsList.Arguments.FirstOrDefault().Str();

            var message = string.Format("Precondition '{0}' is not satisfied!", condition);

            if (argumentsList.Arguments.Count == 2)
            {
                message = argumentsList.Arguments.Skip(1).FirstOrDefault().Str();
            }

            var statementStr = string.Format("if ({0}) {{ throw new Exception({1}); }}", condition, message);

            return SyntaxFactory.ParseStatement(statementStr);
        }
    }

    internal class EnsuresExpressionStrategy : IContractExpressionStrategy
    {
        public int Priority { get { return 60; } }

        public bool IsMine(ExpressionStatementSyntax exp)
        {
            return exp.Str().StartsWith("Contract.Ensures(");
        }

        public StatementSyntax Transform(ExpressionStatementSyntax exp)
        {
            return SyntaxFactory.EmptyStatement();
        }
    }

    internal class EmptyExpressionStrategy : IContractExpressionStrategy
    {
        public int Priority { get { return 0; } }

        public bool IsMine(ExpressionStatementSyntax exp)
        {
            return true;
        }

        public StatementSyntax Transform(ExpressionStatementSyntax exp)
        {
            return SyntaxFactory.EmptyStatement();
        }
    }
}
