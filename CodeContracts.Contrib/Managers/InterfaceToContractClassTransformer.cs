using CodeContracts.Contrib.Helpers;
using CodeContracts.Contrib.Rewriters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;

namespace CodeContracts.Contrib.Managers
{
    public class InterfaceToContractClassTransformer
    {
        /// <summary>
        /// Performing actions in order to get code contract class out of interface:
        /// Changing interface declaration to abstract class declaration, adding necessary code contract attributes and using statements,
        /// implementing interface properties and methods with inserting comments in places where code contract checks should be done.
        /// </summary>
        /// <param name="interfaceNode">Interface definition which should be transformed.</param>
        /// <param name="interfaceName">Name of the interface that should be transformed.</param>
        /// <param name="contractClassName">Generated contract class name.</param>
        /// <returns>Contract class - string representation</returns>
        public string TransformInterfaceToContractClass(SyntaxNode interfaceNode, string interfaceName, string contractClassName)
        {
            //preparing class-to-interface transformation by adding '_Contract' suffix to interface name and adding interface implementation (: <interface-name>)

            var classNode = new ContractInterfaceDeclarationExtender(contractClassName).Visit(interfaceNode);

            //Removing all trivia: comments etc.

            classNode = new DocumentationTriviaRemover().Visit(classNode);

            //Inserting 'using Microsoft.CodeAnalysis.Diagnostics' namespace needed for code contract attributes.

            classNode = new UsingStatementsExtender(IdentifiersHelper.Namespace_System_Diagnostics_Contracts).Visit(classNode);

            //Attaching contract attribute - [ContractClassFor(typeof(<interface_name>))].

            classNode = new InterfaceDeclarationAttributeExtender(IdentifiersHelper.AttributeName_ContractClassFor, interfaceName, true).Visit(classNode);

            //implementing interface buy turning interface declarations into full default property and method definitions. 

            classNode = new ContractInterfaceImplementor().Visit(classNode);

            //Prettifying the code (indents, spaces etc)

            classNode = Formatter.Format(classNode, MSBuildWorkspace.Create());

            //replacing 'interface' with 'class'

            return classNode.Str().Replace("internal abstract interface", "internal abstract class");
        }

            
    }

    

    
}
