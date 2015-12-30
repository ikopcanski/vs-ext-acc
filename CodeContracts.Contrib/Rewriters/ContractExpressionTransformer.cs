using CodeContracts.Contrib.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CodeContracts.Contrib.Rewriters
{
    internal class ContractExpressionTransformer
    {
        private List<ContractExpressionStrategyBase> _strategies;
        private StatementSyntax _contractCallStatement;
        private StatementSyntax _returnStatement;
        private string _returnVarName;

        public ContractExpressionTransformer(StatementSyntax contractCallStatement, StatementSyntax returnStatement, string returnVarName)
        {
            _strategies = new List<ContractExpressionStrategyBase>()
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

        /// <summary>
        /// Transforms list of Contract.Requires<T>(...) and/or Contract.Requires(...) statements into 'if' statements that throw corresponding exception. 
        /// Embeds the contract method call statement and return value statement, so result is complete method body with statements.
        /// </summary>
        /// <param name="expressions">List of expressions that should be transformed.</param>
        /// <returns>complete method body with statements.</returns>
        public IEnumerable<StatementSyntax> Transform(IEnumerable<ExpressionStatementSyntax> expressions)
        {
            var oldValueAssignments = new List<StatementSyntax>();
            var preConditionStatements = new List<StatementSyntax>();
            var postConditionStatements = new List<StatementSyntax>();

            foreach (var expression in expressions)
            {
                var transform = Transform(expression);

                oldValueAssignments.AddRange(transform.OldValueAssignments);

                if (transform.IsPreCondition)
                {
                    preConditionStatements.Add(transform.Transform);
                }
                else
                {
                    postConditionStatements.Add(transform.Transform);
                }
            }

            var retVal = oldValueAssignments.Union(preConditionStatements)
                                            .Union(new[] { _contractCallStatement })
                                            .Union(postConditionStatements);
            if (_returnStatement != null)
            {
                retVal = retVal.Union(new[] { _returnStatement });
            }

            return retVal;          
        }

        private ContractStatementTransform Transform(ExpressionStatementSyntax exp)
        {
            ContractStatementTransform retVal = null;

            foreach (var strategy in _strategies)
            {
                if (strategy.IsMine(exp))
                {
                    retVal = strategy.Transform(exp, _returnVarName);
                    break;
                }
            }

            return retVal;
        }
    }

    internal class ContractStatementTransform
    {
        public StatementSyntax Transform { get; set; }

        public bool IsPreCondition { get; set; }

        public IEnumerable<StatementSyntax> OldValueAssignments { get; set; }
    }

    internal abstract class ContractExpressionStrategyBase
    {
        public abstract int Priority { get; }
        public abstract bool IsMine(ExpressionStatementSyntax exp);
        public abstract ContractStatementTransform Transform(ExpressionStatementSyntax exp, string returnVarName);

        protected IEnumerable<string> ExtractValidations(string condition)
        {
            var matches = Regex.Matches(condition, @"Contract.OldValue<\w+>\(\w+\)");
            foreach (Match oldValueMatch in matches)
            {
                yield return oldValueMatch.Value;
            }
            matches = Regex.Matches(condition, @"Contract.OldValue\(\w+\)");
            foreach (Match oldValueMatch in matches)
            {
                yield return oldValueMatch.Value;
            }
        }

        protected string ReplaceValidations(string condition, IEnumerable<string> validations)
        {
            var retVal = condition;

            foreach (var validation in validations)
            {
                var idendifier = Regex.Match(validation, @"\(\w+\)").Value.Trim('(', ')');
                
                if (validation.StartsWith("Contract.OldValue"))
                {
                    var oldValue = string.Format("{0}_old", idendifier);
                    retVal = retVal.Replace(validation, oldValue);
                }
            }

            return retVal;
        }

        protected IEnumerable<StatementSyntax> GetOldValueAssignments(IEnumerable<string> validations)
        {
            foreach (var validation in validations)
            {
                var idendifier = Regex.Match(validation, @"\(\w+\)").Value.Trim('(', ')');

                if (validation.StartsWith("Contract.OldValue"))
                {
                    var assignment = string.Format("var {0}_old = {1};", idendifier, idendifier);
                    yield return SyntaxFactory.ParseStatement(assignment);
                }
            }
        }
    }

    internal class GenericRequiresExpressionStrategy : ContractExpressionStrategyBase
    {
        public override int Priority { get { return 100; } }

        public override bool IsMine(ExpressionStatementSyntax exp)
        {
            return exp.Str().StartsWith("Contract.Requires<");
        }

        public override ContractStatementTransform Transform(ExpressionStatementSyntax exp, string returnVarName)
        {
            var exceptionType = exp.FirstChild<GenericNameSyntax>()
                                   .ChildrenOfType<IdentifierNameSyntax>()
                                   .FirstOrDefault().Str();

            var argumentsList = exp.FirstChild<ArgumentListSyntax>();

            var condition = argumentsList.Arguments.FirstOrDefault().Str();

            var validations = ExtractValidations(condition);

            condition = ReplaceValidations(condition, validations);

            var message = string.Format("\"Precondition ({0}) is not satisfied!\"", condition.Replace('"', '\''));
            
            if (argumentsList.Arguments.Count == 2)
            {
                message = argumentsList.Arguments.Skip(1).FirstOrDefault().Str();
            }

            var statementStr = string.Format("if (!({0})) \r\n {{ \r\n throw new {1}({2}); \r\n }}", condition, exceptionType, message);

            return new ContractStatementTransform
            {
                Transform = SyntaxFactory.ParseStatement(statementStr),
                IsPreCondition = !exp.Str().Contains("Contract.Result"),
                OldValueAssignments = GetOldValueAssignments(validations)
            };
        }
    }

    internal class SimpleRequiresExpressionStrategy : ContractExpressionStrategyBase
    {
        public override int Priority { get { return 80; } }
        
        public override bool IsMine(ExpressionStatementSyntax exp)
        {
            return exp.Str().StartsWith("Contract.Requires(");
        }

        public override ContractStatementTransform Transform(ExpressionStatementSyntax exp, string returnVarName)
        {
            var argumentsList = exp.FirstChild<ArgumentListSyntax>();

            var condition = argumentsList.Arguments.FirstOrDefault().Str();

            var validations = ExtractValidations(condition);

            condition = ReplaceValidations(condition, validations);

            var message = string.Format("\"Precondition ({0}) is not satisfied!\"", condition.Replace('"', '\''));

            if (argumentsList.Arguments.Count == 2)
            {
                message = argumentsList.Arguments.Skip(1).FirstOrDefault().Str();
            }

            var statementStr = string.Format("if (!({0})) \r\n {{ \r\n throw new Exception({1}); \r\n }}", condition, message);

            return new ContractStatementTransform
            {
                Transform = SyntaxFactory.ParseStatement(statementStr),
                IsPreCondition = !exp.Str().Contains("Contract.Result"),
                OldValueAssignments = GetOldValueAssignments(validations)
            };
        }
    }

    internal class EnsuresExpressionStrategy : ContractExpressionStrategyBase
    {
        public override int Priority { get { return 60; } }

        public override bool IsMine(ExpressionStatementSyntax exp)
        {
            return exp.Str().StartsWith("Contract.Ensures(");
        }

        public override ContractStatementTransform Transform(ExpressionStatementSyntax exp, string returnVarName)
        {
            var argumentsList = exp.FirstChild<ArgumentListSyntax>();

            var condition = argumentsList.Arguments.FirstOrDefault().Str();

            condition = Regex.Replace(condition, @"Contract.Result<\w+>\(\)", returnVarName);

            var validations = ExtractValidations(condition);

            condition = ReplaceValidations(condition, validations);

            var message = string.Format("\"Postcondition ({0}) is not satisfied!\"", condition.Replace('"', '\''));

            if (argumentsList.Arguments.Count == 2)
            {
                message = argumentsList.Arguments.Skip(1).FirstOrDefault().Str();
            }

            var statementStr = string.Format("if (!({0})) \r\n {{ \r\n throw new Exception({1}); \r\n }}", condition, message);

            return new ContractStatementTransform
            {
                Transform = SyntaxFactory.ParseStatement(statementStr),
                IsPreCondition = false,
                OldValueAssignments = GetOldValueAssignments(validations)
            };
        }
    }

    internal class DefaultExpressionStrategy : ContractExpressionStrategyBase
    {
        public override int Priority { get { return 0; } }

        public override bool IsMine(ExpressionStatementSyntax exp) { return exp.Str().StartsWith("Contract."); ; }
        
        public override ContractStatementTransform Transform(ExpressionStatementSyntax exp, string returnVarName)
        {
            return new ContractStatementTransform
            {
                Transform = exp,
                IsPreCondition = true,
                OldValueAssignments = new StatementSyntax[0]
            };
        }
    }

    
}
