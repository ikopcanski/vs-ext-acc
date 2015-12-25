using CodeContractsContrib.Rewriters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace CodeContractsContrib.Managers
{
    public class InterfaceCCAdapter
    {
        /// <summary>
        /// Addapts interface definition for joint usage with generated code contract class.
        /// Adding using statements and attributes necessary for coupling with code contract class.
        /// </summary>
        /// <param name="interfaceNode">Interface definition to be addapted.</param>
        // <returns>Adapted interface definition - string representation</returns>
        public string GetAddaptedInterfaceForCC(SyntaxNode interfaceNode)
        {
            //Inserting 'using Microsoft.CodeAnalysis.Diagnostics' namespace needed for code contract attributes.

            var classNode = new UsingStatementsExtender("System.Diagnostics.Contracts").Visit(interfaceNode);

            //Attaching code contract attribute - [ContractClass(typeof(<interface_name>_CodeContract))].

            var interfaceName = interfaceNode.DescendantNodes().OfType<InterfaceDeclarationSyntax>().First().Identifier.Text.Trim();

            classNode = new AttributeInterfaceDeclarationExtender("ContractClass", interfaceName + InterfaceCCTransformer.CCClassNameSuffix, true).Visit(classNode);

            //Formatting (empty spaces, indents etc)

            return classNode.NormalizeWhitespace().ToFullString();

            
        }
    }

    

    
}
