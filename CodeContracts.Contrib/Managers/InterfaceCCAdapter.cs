using CodeContracts.Contrib.Helpers;
using CodeContracts.Contrib.Rewriters;
using Microsoft.CodeAnalysis;

namespace CodeContracts.Contrib.Managers
{
    public class InterfaceCCAdapter
    {
        /// <summary>
        /// Addapts interface definition for joint usage with generated code contract class.
        /// Adding using statements and attributes necessary for coupling with code contract class.
        /// </summary>
        /// <param name="interfaceNode">Interface definition to be addapted.</param>
        /// <param name="contractClassName">Generated contract class name.</param>
        // <returns>Adapted interface definition - string representation</returns>
        public string GetAddaptedInterfaceForCC(SyntaxNode interfaceNode, string contractClassName)
        {
            //Inserting 'using Microsoft.CodeAnalysis.Diagnostics' namespace needed for code contract attributes.

            var classNode = new UsingStatementsExtender(IdentifiersHelper.Attribute_Namespace).Visit(interfaceNode);

            //Attaching code contract attribute - [ContractClass(typeof(<interface_name>_Contract))].

            classNode = new AttributeInterfaceDeclarationExtender(IdentifiersHelper.AttributeName_ContractClass, contractClassName, true).Visit(classNode);

            //Formatting (empty spaces, indents etc)

            return classNode.NormalizeWhitespace().ToFullString();
        }
    }

    

    
}
