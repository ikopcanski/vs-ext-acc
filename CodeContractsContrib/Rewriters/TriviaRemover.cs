using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeContractsContrib.Rewriters
{
    public class DocumentationTriviaRemover : CSharpSyntaxRewriter
    {
        public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
        {
            return default(SyntaxTrivia);
        }
    }
}
