using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace CodeContracts.Contrib.Helpers
{
    public static class Extensions
    {
        public static IEnumerable<T> ChildrenOfType<T>(this SyntaxNode node) where T : SyntaxNode
        {
            return node.DescendantNodes().OfType<T>();
        }
        
        public static T FirstChild<T>(this SyntaxNode node) where T : SyntaxNode
        {
            return node.DescendantNodes().OfType<T>().FirstOrDefault();
        }
        
        public static string Str(this SyntaxNode node)
        {
            return node.ToFullString().Trim();
        }

        public static string Str(this SyntaxToken token)
        {
            return token.ToFullString().Trim();
        }
    }
}
