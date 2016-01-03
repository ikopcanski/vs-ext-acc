//------------------------------------------------------------------------------
// <copyright file="AddAllContractProxies.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using CodeContracts.Contrib.Helpers;
using CodeContracts.Contrib.Managers;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;

namespace CodeContracts.Contrib
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class AddAllContractProxies
    {
        #region [ Props, ctor and init ]

        public static readonly Guid CommandSet = new Guid("5278d6da-9d41-4281-9f70-aa8bcac9960c");

        public const int CommandId = 256;

        private readonly Package package;

        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        public static AddAllContractProxies Instance
        {
            get;
            private set;
        }

        public static void Initialize(Package package)
        {
            Instance = new AddAllContractProxies(package);
        }

        private AddAllContractProxies(Package package)
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
        private async void MenuItemCallback(object sender, EventArgs e)
        {

            var myCommand = sender as OleMenuCommand;
            if (myCommand != null)
            {
                myCommand.Text = "Create All Contract Proxies";
            }

            await GenerateAllContractProxiesAsync();
        }

        #endregion

        private async System.Threading.Tasks.Task GenerateAllContractProxiesAsync()
        {
            try
            {
                var items = VSModelHelper.GetAllCodeContracts(ServiceProvider);

                //Creates proxy class for code contract class and saves it to the file. Transforms Contract.* directives in to if-then-throw statements.

                var proxyCreator = new ContractProxyCreator();
                
                foreach (var item in items)
                {
                    await proxyCreator.CreateContractProxyAsync(ServiceProvider, item);
                }
            }
            catch (Exception ex)
            {
                var message = string.Format("Exception occured while generating all contract proxies for current solution: {0}", ex.Message);
                VSModelHelper.ShowMessage(ServiceProvider, "Command Error", message);
            }
        }
    }
}
