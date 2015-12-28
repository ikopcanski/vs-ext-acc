using CodeContracts.Contrib.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CodeContracts.Contrib.Managers
{
    internal class ContractExpressionTransformer
    {
        private List<IContractExpressionStrategy> _strategies;
        private StatementSyntax _contractCallStatement;
        private StatementSyntax _returnStatement;
        private string _returnVarName;

        public ContractExpressionTransformer(StatementSyntax contractCallStatement, StatementSyntax returnStatement, string returnVarName)
        {
            _strategies = new List<IContractExpressionStrategy>()
            {
                new GenericRequiresExpressionStrategy(),
                new SimpleRequiresExpressionStrategy(),
                new EnsuresExpressionStrategy(),
                new DefaultExpressionStrategy()
            }
            .OrderByDescending(s => s.Priority).ToList();
            _contractCallStatement = contractCallStatement;
            _returnStatement = returnStatement;
            _returnVarName = returnVarName;
        }

        private Tuple<StatementSyntax, bool> Transform(ExpressionStatementSyntax exp)
        {
            StatementSyntax retVal = null;
            bool isPreCondition = false;

            foreach (var strategy in _strategies)
            {
                if (strategy.IsMine(exp))
                {
                    retVal = strategy.Transform(exp, _returnVarName);
                    isPreCondition = strategy.IsPreCondition(exp);
                    break;
                }
            }

            return new Tuple<StatementSyntax, bool>(retVal, isPreCondition);
        }

        /// <summary>
        /// Transforms list of Contract.Requires<T>(...) and/or Contract.Requires(...) statements into 'if' statements that throw corresponding exception. 
        /// Embeds the contract method call statement and return value statement, so result is complete method body with statements.
        /// </summary>
        /// <param name="expressions">List of expressions that should be transformed.</param>
        /// <returns>complete method body with statements.</returns>
        public IEnumerable<StatementSyntax> Transform(IEnumerable<ExpressionStatementSyntax> expressions)
        {
            var preConditionStatements = new List<StatementSyntax>();
            var postConditionStatements = new List<StatementSyntax>();

            foreach (var expression in expressions)
            {
                var statement = Transform(expression);
                if (statement.Item2)
                {
                    preConditionStatements.Add(statement.Item1);
                }
                else
                {
                    postConditionStatements.Add(statement.Item1);
                }
            }

            var retVal = preConditionStatements.Union(new[] { _contractCallStatement }).Union(postConditionStatements);
            if (_returnStatement != null)
            {
                retVal = retVal.Union(new[] { _returnStatement });
            }

            return retVal;          
        }
    }

    internal interface IContractExpressionStrategy
    {
        int Priority { get; }
        bool IsMine(ExpressionStatementSyntax exp);
        bool IsPreCondition(ExpressionStatementSyntax exp);
        StatementSyntax Transform(ExpressionStatementSyntax exp, string returnVarName);
    }

    internal class GenericRequiresExpressionStrategy : IContractExpressionStrategy
    {
        public int Priority { get { return 100; } }

        public bool IsMine(ExpressionStatementSyntax exp)
        {
            return exp.Str().StartsWith("Contract.Requires<");
        }

        public bool IsPreCondition(ExpressionStatementSyntax exp) { return true; }

        public StatementSyntax Transform(ExpressionStatementSyntax exp, string returnVarName)
        {
            var exceptionType = exp.FirstChild<GenericNameSyntax>()
                                   .ChildrenOfType<IdentifierNameSyntax>()
                                   .FirstOrDefault().Str();

            var argumentsList = exp.FirstChild<ArgumentListSyntax>();

            var condition = argumentsList.Arguments.FirstOrDefault().Str();

            var message = string.Format("\"Precondition '{0}' is not satisfied!\"", condition);
            
            if (argumentsList.Arguments.Count == 2)
            {
                message = argumentsList.Arguments.Skip(1).FirstOrDefault().Str();
            }

            var statementStr = string.Format("if (!({0})) \r\n {{ \r\n throw new {1}({2}); \r\n }}", condition, exceptionType, message);

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

        public bool IsPreCondition(ExpressionStatementSyntax exp) { return true; }

        public StatementSyntax Transform(ExpressionStatementSyntax exp, string returnVarName)
        {
            var argumentsList = exp.FirstChild<ArgumentListSyntax>();

            var condition = argumentsList.Arguments.FirstOrDefault().Str();

            var message = string.Format("\"Precondition '{0}' is not satisfied!\"", condition);

            if (argumentsList.Arguments.Count == 2)
            {
                message = argumentsList.Arguments.Skip(1).FirstOrDefault().Str();
            }

            var statementStr = string.Format("if (!({0})) \r\n {{ \r\n throw new Exception({1}); \r\n }}", condition, message);

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

        public bool IsPreCondition(ExpressionStatementSyntax exp) { return false; }

        public StatementSyntax Transform(ExpressionStatementSyntax exp, string returnVarName)
        {
            var argumentsList = exp.FirstChild<ArgumentListSyntax>();

            var condition = argumentsList.Arguments.FirstOrDefault().Str();

            condition = Regex.Replace(condition, @"Contract.Result<\w+>\(\)", returnVarName);

            var message = string.Format("\"Postcondition '{0}' is not satisfied!\"", condition);

            if (argumentsList.Arguments.Count == 2)
            {
                message = argumentsList.Arguments.Skip(1).FirstOrDefault().Str();
            }

            var statementStr = string.Format("if (!({0})) \r\n {{ \r\n throw new Exception({1}); \r\n }}", condition, message);

            return SyntaxFactory.ParseStatement(statementStr);
        }
    }

    internal class DefaultExpressionStrategy : IContractExpressionStrategy
    {
        public int Priority { get { return 0; } }

        public bool IsMine(ExpressionStatementSyntax exp) { return true; }

        public bool IsPreCondition(ExpressionStatementSyntax exp) { return false; }

        public StatementSyntax Transform(ExpressionStatementSyntax exp, string returnVarName)
        {
            return SyntaxFactory.EmptyStatement();
        }
    }
}
