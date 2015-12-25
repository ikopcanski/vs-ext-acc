using CodeContracts.Contrib.Rewriters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;
using System.Linq;

namespace CodeContracts.Contrib.Managers
{
    public class InterfaceCCTransformer
    {
        public const string CCGeneratedFileSuffix = ".contract";
        public const string CCClassNameSuffix = "_Contract";
        public const string Attribute_Namespace = "System.Diagnostics.Contracts";
        public const string AttributeName_ContractClassFor = "ContractClassFor";
        public const string AttributeName_ContractClass = "ContractClass";

        /// <summary>
        /// Performing actions in order to get code contract class out of interface:
        /// Changing interface declaration to abstract class declaration, adding necessary code contract attributes and using statements,
        /// implementing interface properties and methods with inserting comments in places where code contract checks should be done.
        /// </summary>
        /// <param name="interfaceNode">Interface definition which should be transformed.</param>
        /// <returns>Contract class - string representation</returns>
        public string GetCodeContractClass(SyntaxNode interfaceNode)
        {
            //preparing interface declaration by adding '_cc' suffix to interface name and adding interface implementation (: <interface-name>)

            var classNode = new CCInterfaceDeclarationExtender(CCClassNameSuffix).Visit(interfaceNode);

            //Removing all trivia: comments etc.

            classNode = new DocumentationTriviaRemover().Visit(classNode);

            //Inserting 'using Microsoft.CodeAnalysis.Diagnostics' namespace needed for code contract attributes.

            classNode = new UsingStatementsExtender(Attribute_Namespace).Visit(classNode);

            //Attaching contract attribute - [ContractClassFor(typeof(<interface_name>))].

            var interfaceName = interfaceNode.DescendantNodes().OfType<InterfaceDeclarationSyntax>().First().Identifier.Text.Trim();

            classNode = new AttributeInterfaceDeclarationExtender(AttributeName_ContractClassFor, interfaceName, true).Visit(classNode);

            //implementing interface buy turning interface declarations into full default property and method definitions. 

            classNode = new CCInterfaceImplementor().Visit(classNode);

            //Prettifying the code (indents, spaces etc)

            classNode = Formatter.Format(classNode, MSBuildWorkspace.Create());

            //replacing 'interface' with 'class'

            return classNode.ToFullString().Replace("internal abstract interface", "internal abstract class");
        }

        public static string GetGeneratedClassName(string interfaceName)
        {
            return interfaceName + InterfaceCCTransformer.CCClassNameSuffix;
        }
    }

    

    
}
