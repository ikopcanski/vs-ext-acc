//------------------------------------------------------------------------------
// <copyright file="AddCodeContract.cs" company="Kopalite">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using CodeContracts.Contrib.Helpers;
using CodeContracts.Contrib.Managers;
using Microsoft.CodeAnalysis;
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
    internal sealed class AddCodeContract
    {
        #region [ Props, ctor and init ]

        public static readonly Guid CommandSet = new Guid("8a37555e-23b6-4c43-b200-702b66b0326d");

        public const int CommandId = 0x0100;

        private readonly Package package;

        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        public static AddCodeContract Instance
        {
            get;
            private set;
        }

        public static void Initialize(Package package)
        {
            Instance = new AddCodeContract(package);
        }
        
        private AddCodeContract(Package package)
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
                myCommand.Text = "Add Code Contract";
            }

            GenerateCodeContractClassFile();
        }

        #endregion

        private void GenerateCodeContractClassFile()
        {
            try
            {
                var items = VSModelHelper.GetSelectedItems(ServiceProvider);

                if (items.Count() != 1)
                {
                    VSModelHelper.ShowMessage(ServiceProvider, "Command Error", "Please select single file containing only one interface definition.");
                    return;
                }

                //Loading and analyzing syntax tree of selected file.

                var item = items.First();
                var filePath = item.Properties.Item("FullPath").Value.ToString();
                var sourceCode = File.ReadAllText(filePath);
                var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
                var rootNode = syntaxTree.GetRoot();
                var interfaces = rootNode.DescendantNodes().OfType<InterfaceDeclarationSyntax>().ToArray();
                var classes = rootNode.DescendantNodes().OfType<ClassDeclarationSyntax>().ToArray();

                //This command can work on only one interface declaration at the time.

                if (interfaces.Count() != 1 || classes.Count() > 0)
                {
                    VSModelHelper.ShowMessage(ServiceProvider, "Command Error", "Please select single file containing only one interface definition.");
                    return;
                }

                //Determining interface name and forming the generated class name and the new file that will be created.

                var interfaceName = rootNode.DescendantNodes().OfType<InterfaceDeclarationSyntax>().First().Identifier.Text.Trim();
                var contractClassName = IdentifiersHelper.GetCodeContractClassName(interfaceName);
                var contractClassFile = GetCodeContractClassFilePath(filePath);

                //Taking the interface syntax node and creating abstract class as code contract for it.

                var codeContractClass = new InterfaceToContractClassTransformer().TransformInterfaceToContractClass(rootNode, interfaceName, contractClassName);
                File.WriteAllText(contractClassFile, codeContractClass);

                //Adapting interface by coupling it with generated code contract class (adding namespace and attribute).

                var adaptedInterface = new InterfaceToContractClassAdapter().AdaptInterfaceForContractClass(rootNode, contractClassName);
                File.WriteAllText(filePath, adaptedInterface);

                //Adding generated file to project and nesting it under the interface file.

                item.ProjectItems.AddFromFile(contractClassFile);
            }
            catch (Exception ex)
            {
                var message = string.Format("Exception occured while generating code contract class:\r\n{0}", ex.Message);
                VSModelHelper.ShowMessage(ServiceProvider, "Command Error", message);
            }
        }

        private string GetCodeContractClassFilePath(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);
            return string.Format(@"{0}\{1}{2}", directory, IdentifiersHelper.GetCodeContractClassFile(fileName), extension);
        }
    }
}
