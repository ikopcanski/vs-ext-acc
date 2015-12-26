//------------------------------------------------------------------------------
// <copyright file="AddContractProxy.cs" company="Kopalite">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using CodeContracts.Contrib.Helpers;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;

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

            VSModelHelper.ShowMessage(ServiceProvider, "Test", "Test");
        }

        #endregion
    }
}
