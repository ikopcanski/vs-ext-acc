//------------------------------------------------------------------------------
// <copyright file="AddProxyAttribute.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using CodeContracts.Contrib.Helpers;
using System.Linq;
using System.IO;



namespace CodeContracts.Contrib
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class AddProxyAttribute
    {
        #region [ Props, ctor and init ]

        public static readonly Guid CommandSet = new Guid("61063f2f-f3b9-4140-8fae-2ed8036d9640");

        public const int CommandId = 256;

        private readonly Package package;

        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        public static AddProxyAttribute Instance
        {
            get;
            private set;
        }

        public static void Initialize(Package package)
        {
            Instance = new AddProxyAttribute(package);
        }

        private AddProxyAttribute(Package package)
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

            GenerateContractProxyAttributeFile();
        }

        #endregion

        private void GenerateContractProxyAttributeFile()
        {
            var items = VSModelHelper.GetSelectedProjects(ServiceProvider).ToArray();

            if (items.Count() != 1)
            {
                VSModelHelper.ShowMessage(ServiceProvider, "Command Error", "Please select only one project in order to create ContractProxyAttribute class.");
                return;
            }

            var item = items.First();
            var projectDir = item.Properties.Item("FullPath").Value.ToString();
            var attributeFile = string.Format(@"{0}\{1}{2}", projectDir, "ContractProxyAttribute", ".cs");

            string attributeCode = @"using System;
namespace CodeContracts.Contrib
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class ContractProxyAttribute : Attribute
    {
        public Type ContractType { get; private set; }

        public ContractProxyAttribute(Type contractType)
        {
            ContractType = contractType;
        }
    }
}";

            //File.WriteAllText(attributeFile, attributeCode);

            item. ProjectItems.AddFromFile(attributeFile);
        }
    }
}


