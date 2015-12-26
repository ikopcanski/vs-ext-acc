//------------------------------------------------------------------------------
// <copyright file="AddContractProxy.cs" company="Kopalite">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using CodeContracts.Contrib.Helpers;
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
                var classes = rootNode.DescendantNodes().OfType<ClassDeclarationSyntax>().ToArray();
                var interfaces = rootNode.DescendantNodes().OfType<InterfaceDeclarationSyntax>().ToArray();

                //This command can work on only one class declaration at the time.

                if (classes.Count() != 1 || interfaces.Count() > 0)
                {
                    VSModelHelper.ShowMessage(ServiceProvider, "Command Error", "Please select single file containing generated code contract class (result of 'Create Code Contract' command).");
                    return;
                }

                //Determining interface name and forming the generated class name and the new file that will be created.

                var contractClassName = rootNode.DescendantNodes().OfType<ClassDeclarationSyntax>().First().Identifier.Text.Trim();
                var proxyClassName = IdentifiersHelper.GetCodeContractClassName(contractClassName);
                var proxyClassFile = GetCodeContractClassFilePath(filePath);

                //Taking the interface syntax node and creating abstract class as code contract for it.

                //var contactProxyClass = new InterfaceCCTransformer().GetCodeContractClass(rootNode, proxyClassName);
                //File.WriteAllText(contractClassFile, codeContractClass);

                //Adapting interface by coupling it with generated code contract class (adding namespace and attribute).

                //var adaptedInterface = new InterfaceCCAdapter().GetAddaptedInterfaceForCC(rootNode, contractClassName);
                //File.WriteAllText(filePath, adaptedInterface);

                //Adding generated file to project and nesting it under the interface file.

                //item.ProjectItems.AddFromFile(contractClassFile);
            }
            catch (Exception ex)
            {
                var message = string.Format("Exception occured while generating contract proxy class:\r\n{0}", ex.Message);
                VSModelHelper.ShowMessage(ServiceProvider, "Command Error", message);
            }
        }

        private string GetCodeContractClassFilePath(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);
            return string.Format(@"{0}\{1}{2}", directory, IdentifiersHelper.GetContractProxyClassFile(fileName), extension);
        }
    }
}
