//------------------------------------------------------------------------------
// <copyright file="AddCodeContract.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using CodeContracts.Contrib.Managers;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
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
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("8a37555e-23b6-4c43-b200-702b66b0326d");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddCodeContract"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
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
        /// Gets the instance of the command.
        /// </summary>
        public static AddCodeContract Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new AddCodeContract(package);
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

            var items = GetSelectedItems();
            GenerateContractFile(items);
        }

        private void GenerateContractFile(IEnumerable<ProjectItem> items)
        {
            try
            {
                if (items.Count() != 1)
                {
                    ShowMessage("Command Error", "Please select single file containing only one interface definition.");
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
                    ShowMessage("Command Error", "Please select single file containing only one interface definition.");
                    return;
                }

                //Determining interface name and forming the generated class name and the new file that will be created.

                var interfaceName = rootNode.DescendantNodes().OfType<InterfaceDeclarationSyntax>().First().Identifier.Text.Trim();
                var contractClassName = IdentifiersHelper.GetGeneratedClassName(interfaceName);
                var contractClassFile = GetGeneratedFilePath(filePath);

                //Taking the interface syntax node and creating abstract class as code contract for it.

                var codeContractClass = new InterfaceCCTransformer().GetCodeContractClass(rootNode, interfaceName, contractClassName);
                File.WriteAllText(contractClassFile, codeContractClass);

                //Adapting interface by coupling it with generated code contract class (adding namespace and attribute).

                var adaptedInterface = new InterfaceCCAdapter().GetAddaptedInterfaceForCC(rootNode, contractClassName);
                File.WriteAllText(filePath, adaptedInterface);

                //Adding generated file to project and nesting it under the interface file.

                item.ProjectItems.AddFromFile(contractClassFile);
            }
            catch (Exception ex)
            {
                var message = string.Format("Exception occured while generating code contract class:\r\n{0}", ex.Message);
                ShowMessage("Command Error", message);
            }
        }

        private IEnumerable<ProjectItem> GetSelectedItems()
        {
            var appObject = ServiceProvider.GetService(typeof(DTE)) as EnvDTE80.DTE2;
            var explorer = appObject.ToolWindows.SolutionExplorer;
            Array selectedItems = (Array)explorer.SelectedItems;
            if (selectedItems != null)
            {
                foreach (UIHierarchyItem selectedItem in selectedItems)
                {
                    yield return selectedItem.Object as ProjectItem;
                }
            }
        }

        private string GetGeneratedFilePath(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);
            return string.Format(@"{0}\{1}{2}", directory, IdentifiersHelper.GetGeneratedClassFile(fileName), extension);
        }

        private void ShowMessage(string title, string message)
        {
            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                this.ServiceProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
        













    }
}
