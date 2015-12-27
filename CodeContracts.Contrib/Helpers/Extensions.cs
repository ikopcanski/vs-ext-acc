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

        public static string SubString(this string str, char openningChar, char closingChar)
        {
            if (str.Length <= 2)
            {
                return "";
            }

            var openningIndex = str.IndexOf(openningChar, 0);
            var closingIndex = str.IndexOf(closingChar, 0);

            if (openningIndex < 0 || closingIndex < 0 || openningIndex >= closingIndex || openningIndex + 1 == str.Length)
            {
                return "";
            }
            
            return str.Substring(openningIndex + 1, closingIndex - openningIndex - 1);
        }
    }
}
