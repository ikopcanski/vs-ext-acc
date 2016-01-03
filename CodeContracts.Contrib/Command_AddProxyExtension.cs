//------------------------------------------------------------------------------
// <copyright file="AddContractProxyExtension.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using CodeContracts.Contrib.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CodeContracts.Contrib
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class AddContractProxyExtension
    {
        #region [ Props, ctor and init ]

        public static readonly Guid CommandSet = new Guid("459931a1-066a-4950-83d5-16d94e80d212");

        public const int CommandId = 256;

        private readonly Package package;

        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        public static AddContractProxyExtension Instance
        {
            get;
            private set;
        }

        public static void Initialize(Package package)
        {
            Instance = new AddContractProxyExtension(package);
        }

        private AddContractProxyExtension(Package package)
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
                myCommand.Text = "Create Contract Proxy Extension";
            }

            GenerateContractProxyExtensionFile();
        }

        #endregion

        private void GenerateContractProxyExtensionFile()
        {
            var items = VSModelHelper.GetSelectedProjects(ServiceProvider).ToArray();

            if (items.Count() != 1)
            {
                VSModelHelper.ShowMessage(ServiceProvider, "Command Error", "Please select only one project in order to create ContractProxyExtension class.");
                return;
            }

            var item = items.First();
            var projectDir = item.Properties.Item("FullPath").Value.ToString();
            var extensionFile = string.Format(@"{0}{1}{2}", projectDir, "ContractProxyExtension", ".cs");

            string extensionCode = @"using System;
                                    using System.Collections.Generic;
                                    using System.Linq;
                                    using System.Reflection;

                                    namespace CodeContracts.Contrib
                                    {
                                        public static class ContractProxyExtensions
                                        {
                                            public static IEnumerable<ContractProxyDefinition> GetAllContractProxies(this Assembly assembly)
                                            {
                                                var proxyTypes = assembly.GetTypes().Where(t => t.GetCustomAttribute<ContractProxyAttribute>() != null);

                                                foreach (var proxyType in proxyTypes)
                                                {
                                                    var attribute = proxyType.GetCustomAttribute<ContractProxyAttribute>();
                                                    yield return new ContractProxyDefinition(attribute.ContractType, proxyType);
                                                }
                                            }
                                        }

                                        public class ContractProxyDefinition
                                        {
                                            public Type ContractType { get; private set; }

                                            public Type ContractProxyType { get; private set; }

                                            public ContractProxyDefinition(Type contractType, Type contractProxyType)
                                            {
                                                ContractType = contractType;
                                                ContractProxyType = contractProxyType;
                                            }
                                        }
                                    }";

            var extensionNode = CSharpSyntaxTree.ParseText(extensionCode).GetRoot();
            extensionCode = Formatter.Format(extensionNode, MSBuildWorkspace.Create()).Str();

            File.WriteAllText(extensionFile, extensionCode);

            item.ProjectItems.AddFromFile(extensionFile);
        }
    }

    
}
