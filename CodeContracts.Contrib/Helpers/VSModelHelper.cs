using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeContracts.Contrib.Helpers
{
    internal class VSModelHelper
    {
        public static IEnumerable<ProjectItem> GetSelectedItems(IServiceProvider provider)
        {
            var appObject = provider.GetService(typeof(DTE)) as EnvDTE80.DTE2;
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

        public static IEnumerable<Project> GetSelectedProjects(IServiceProvider provider)
        {
            var appObject = provider.GetService(typeof(DTE)) as EnvDTE80.DTE2;
            var explorer = appObject.ToolWindows.SolutionExplorer;
            Array selectedItems = (Array)explorer.SelectedItems;
            if (selectedItems != null)
            {
                foreach (UIHierarchyItem selectedItem in selectedItems)
                {
                    yield return selectedItem.Object as Project;
                }
            }
        }

        public static void ShowMessage(IServiceProvider provider, string title, string message)
        {
            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                provider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
