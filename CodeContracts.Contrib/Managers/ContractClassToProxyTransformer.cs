using CodeContracts.Contrib.Helpers;
using CodeContracts.Contrib.Rewriters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;

namespace CodeContracts.Contrib.Managers
{
    internal class ContractClassToProxyTransformer
    {
        public string GetContractProxyClass(SyntaxNode rootNode, string interfaceName, string proxyClassName)
        {
            //Creating "public sealed class <interface-name>_proxy" class declaration.

            var classNode = new ProxyClassDeclarationExtender(proxyClassName, interfaceName).Visit(rootNode);

            //Adding 'using' statement for ContractProxyAttribute.

            classNode = new UsingStatementsExtender(IdentifiersHelper.Namespace_CodeContractsContrib).Visit(classNode);

            //Adding ContractProxyAttribute with interface name as parameter.

            classNode = new ClassDeclarationAttributeExtender(IdentifiersHelper.AttributeName_ContractProxy, interfaceName, true).Visit(classNode);

            //Adding internal field of interface type and constructor that initializes the field with injected value.

            classNode = new ProxyClassConstructorCreator(interfaceName).Visit(classNode);

            //TODO: In method/property declarations: replace Contract.Requires() and Contract.Ensure() statements with if statements that throw Exception(contract message)

            //Prettifying the code (indents, spaces etc)

            classNode = Formatter.Format(classNode, MSBuildWorkspace.Create());

            return classNode.ToFullString();
        }
    }
}
