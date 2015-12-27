//------------------------------------------------------------------------------
// <copyright file="AddContractProxy.cs" company="Kopalite">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using CodeContracts.Contrib.Helpers;
using CodeContracts.Contrib.Managers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;

namespace CodeContracts.Contrib
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class AddContractProxy
    {
        #region [ Props, ctor and init ]

        public static readonly Guid CommandSet = new Guid("19db6c4c-af0a-46b8-80ff-02a586ed1574");

        public const int CommandId = 256;

        private readonly Package package;

        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        public static AddContractProxy Instance
        {
            get;
            private set;
        }

        public static void Initialize(Package package)
        {
            Instance = new AddContractProxy(package);
        }

        private AddContractProxy(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {

            var myCommand = sender as OleMenuCommand;
            if (myCommand != null)
            {
                myCommand.Text = "Add Contract Proxy";
            }

            GenerateContractProxyClassFile();
        }

        #endregion

        private void GenerateContractProxyClassFile()
        {
            try
            {
                var items = VSModelHelper.GetSelectedItems(ServiceProvider);

                if (items.Count() != 1)
                {
                    VSModelHelper.ShowMessage(ServiceProvider, "Command Error", "Please select single file containing generated code contract class (result of 'Create Code Contract' command).");
                    return;
                }

                //Loading and analyzing syntax tree of selected file.

                var item = items.First();
                var filePath = item.Properties.Item("FullPath").Value.ToString();
                var sourceCode = File.ReadAllText(filePath);
                var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
                var rootNode = syntaxTree.GetRoot();
                var classes = rootNode.ChildrenOfType<ClassDeclarationSyntax>().ToArray();
                var interfaces = rootNode.ChildrenOfType<InterfaceDeclarationSyntax>().ToArray();

                //This command can work on only one contract class declaration at the time.

                if (classes.Count() != 1 || interfaces.Count() > 0)
                {
                    VSModelHelper.ShowMessage(ServiceProvider, "Command Error", "Please select single file containing generated code contract class (result of 'Create Code Contract' command).");
                    return;
                }

                //Checking if selected file is class declaration with "[ContractClassFor(...)] attribute.

                var attributeNode = classes.First().ChildrenOfType<AttributeSyntax>().FirstOrDefault(a => a.ToFullString().Contains(IdentifiersHelper.AttributeName_ContractClassFor));
                if (attributeNode == null)
                {
                    VSModelHelper.ShowMessage(ServiceProvider, "Command Error", "Please select single file containing generated code contract class (result of 'Create Code Contract' command).");
                    return;
                }

                //Determining code contract class and interface name - forming the contract proxy class name and the name of the file that will be created.

                var interfaceName = attributeNode.ChildrenOfType<TypeOfExpressionSyntax>().First().Type.ToFullString();
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
            catch (Exception ex)
            {
                var message = string.Format("Exception occured while generating contract proxy class:\r\n{0}", ex.Message);
                VSModelHelper.ShowMessage(ServiceProvider, "Command Error", message);
            }
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
