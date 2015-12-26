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
    }
}
