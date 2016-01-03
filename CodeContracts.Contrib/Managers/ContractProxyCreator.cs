using CodeContracts.Contrib.Helpers;
using EnvDTE;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CodeContracts.Contrib.Managers
{
    internal class ContractProxyCreator
    {
        /// <summary>
        /// Creates proxy class for code contract. Transforms Contract.* directives in to if-then-throw statements
        /// taking into account boolean conditions used in Contract directives.
        /// </summary>
        /// <param name="provider"><see cref="IServiceProvider"/> instance for using VS Extensibility Framework.</param>
        /// <param name="item"><see cref="ProjectItem"/> instance solution explorer file with contract class that should be used for proxy class creation.</param>
        public void CreateContractProxy(IServiceProvider provider, ProjectItem item)
        {
            //Loading and analyzing syntax tree of selected file.

            var filePath = item.Properties.Item("FullPath").Value.ToString();
            var sourceCode = File.ReadAllText(filePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var rootNode = syntaxTree.GetRoot();
            var classes = rootNode.ChildrenOfType<ClassDeclarationSyntax>().ToArray();
            var interfaces = rootNode.ChildrenOfType<InterfaceDeclarationSyntax>().ToArray();

            //This command can work on only one contract class declaration at the time.

            if (classes.Count() != 1 || interfaces.Count() > 0)
            {
                VSModelHelper.ShowMessage(provider, "Command Error", "Please select single file containing generated code contract class (result of 'Create Code Contract' command).");
                return;
            }

            //Checking if selected file is class declaration with "[ContractClassFor(...)] attribute.

            var attributeNode = classes.First().ChildrenOfType<AttributeSyntax>().FirstOrDefault(a => a.Str().Contains(IdentifiersHelper.AttributeName_ContractClassFor));
            if (attributeNode == null)
            {
                VSModelHelper.ShowMessage(provider, "Command Error", "Please select single file containing generated code contract class (result of 'Create Code Contract' command).");
                return;
            }

            //Determining code contract class and interface name - forming the contract proxy class name and the name of the file that will be created.

            var interfaceName = attributeNode.ChildrenOfType<TypeOfExpressionSyntax>().First().Type.Str();
            var contractClassNode = rootNode.ChildrenOfType<ClassDeclarationSyntax>().First();
            var contractClassName = contractClassNode.Identifier.Text.Trim();
            var proxyClassName = IdentifiersHelper.GetContractProxyClassName(contractClassName);
            var proxyClassFile = GetContractProxyClassFilePath(filePath);

            //Taking the code contract class syntax node and creating contract proxy class (replacing Contract.Requires() and Contract.Ensure() statements with 'if' statements).

            var contactProxyClass = new ContractClassToProxyTransformer().TransormToContractProxyClass(rootNode, interfaceName, proxyClassName);
            File.WriteAllText(proxyClassFile, contactProxyClass);

            //Adding generated file to project and nesting it under the code contract file.

            item.ProjectItems.AddFromFile(proxyClassFile);
        }

        /// <summary>
        /// Creates proxy class for code contract. Transforms Contract.* directives in to if-then-throw statements
        /// taking into account boolean conditions used in Contract directives.
        /// </summary>
        /// <param name="provider"><see cref="IServiceProvider"/> instance for using VS Extensibility Framework.</param>
        /// <param name="item"><see cref="ProjectItem"/> instance solution explorer file with contract class that should be used for proxy class creation.</param>
        public async Task CreateContractProxyAsync(IServiceProvider provider, ProjectItem item)
        {
            //Loading and analyzing syntax tree of selected file.

            var filePath = item.Properties.Item("FullPath").Value.ToString();

            string sourceCode = null;
            using (var reader = File.OpenText(filePath))
            {
                sourceCode = await reader.ReadToEndAsync();
            }

            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var rootNode = await syntaxTree.GetRootAsync();
            var classes = rootNode.ChildrenOfType<ClassDeclarationSyntax>().ToArray();
            var interfaces = rootNode.ChildrenOfType<InterfaceDeclarationSyntax>().ToArray();

            //This command can work on only one contract class declaration at the time.

            if (classes.Count() != 1 || interfaces.Count() > 0)
            {
                VSModelHelper.ShowMessage(provider, "Command Error", "Please select single file containing generated code contract class (result of 'Create Code Contract' command).");
                return;
            }

            //Checking if selected file is class declaration with "[ContractClassFor(...)] attribute.

            var attributeNode = classes.First().ChildrenOfType<AttributeSyntax>().FirstOrDefault(a => a.Str().Contains(IdentifiersHelper.AttributeName_ContractClassFor));
            if (attributeNode == null)
            {
                VSModelHelper.ShowMessage(provider, "Command Error", "Please select single file containing generated code contract class (result of 'Create Code Contract' command).");
                return;
            }

            //Determining code contract class and interface name - forming the contract proxy class name and the name of the file that will be created.

            var interfaceName = attributeNode.ChildrenOfType<TypeOfExpressionSyntax>().First().Type.Str();
            var contractClassNode = rootNode.ChildrenOfType<ClassDeclarationSyntax>().First();
            var contractClassName = contractClassNode.Identifier.Text.Trim();
            var proxyClassName = IdentifiersHelper.GetContractProxyClassName(contractClassName);
            var proxyClassFile = GetContractProxyClassFilePath(filePath);

            //Taking the code contract class syntax node and creating contract proxy class (replacing Contract.Requires() and Contract.Ensure() statements with 'if' statements).

            var contactProxyClass = new ContractClassToProxyTransformer().TransormToContractProxyClass(rootNode, interfaceName, proxyClassName);
            
            using (var writer = File.CreateText(proxyClassFile))
            {
                await writer.WriteAsync(contactProxyClass);
            }

            //Adding generated file to project and nesting it under the code contract file.

            item.ProjectItems.AddFromFile(proxyClassFile);
        }

        private string GetContractProxyClassFilePath(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);
            return string.Format(@"{0}\{1}{2}", directory, IdentifiersHelper.GetContractProxyClassFile(fileName), extension);
        }
    }
}
