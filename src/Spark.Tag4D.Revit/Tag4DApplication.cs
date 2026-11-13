using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Spark.Tag4D.Core;
using Spark.Tag4D.Core.Utils;
using Spark.Tag4D.Revit.Core.Models;
using Spark.Tag4D.Revit.App.ProjectSettings;
using Spark.Tag4D.Revit.App.Review;
using Spark.Tag4D.Revit.App.Tag;
using System;
using System.IO;
using System.Reflection;

namespace Spark.Tag4D.Revit
{
    [Transaction(TransactionMode.Manual)]
    public class Tag4DApplication : IExternalApplication
    {
        static Tag4DApplication()
        {
            // Revit loads the add-in from its own folder, so WPF (pack://application:,,,/Spark.Tag4D.UI;component/...) and
            // Revit cannot find the sibling assemblies by name. Resolve them from the folder this assembly lives in.
            AppDomain.CurrentDomain.AssemblyResolve += ResolveFromAddInFolder;
        }

        private static Assembly ResolveFromAddInFolder(object sender, ResolveEventArgs args)
        {
            string name = new AssemblyName(args.Name).Name;
            if (!name.StartsWith("Spark.Tag4D", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string path = Path.Combine(AssemblyUtils.GetFolder(typeof(Tag4DApplication)), name + ".dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        }

        public TagView TagView { get; private set; }
        public ReviewView ReviewView { get; private set; }
        public DockablePaneId TagPaneId { get; private set; }
        public UIControlledApplication UIControlledApplication { get; private set; }

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                UIControlledApplication = application;
                Ribbon ribbon = new Ribbon(application);
                ribbon.AddTab(Constants.TabName);
                RibbonPanel panelIfc = ribbon.AddPanel(Constants.TabName, Constants.PanelNameIfc);

                TagView = new TagView();
                TagPaneId = new DockablePaneId(new Guid(Constants.TagPaneId));
                application.RegisterDockablePane(TagPaneId, "Tag", TagView);
                HidePane(application, TagPaneId);

                ReviewView = new ReviewView();
                application.RegisterDockablePane(new DockablePaneId(new Guid(Constants.ReviewPaneId)), "Review", ReviewView);

                application.ViewActivated -= ApplicationViewActivated;
                application.ViewActivated += ApplicationViewActivated;
                string assemblyFolder = AssemblyUtils.GetFolder(typeof(Tag4DApplication));
                string commandsAssemblyPath = typeof(SettingsCommand).Assembly.Location;

                AddButton(ribbon, panelIfc, commandsAssemblyPath, assemblyFolder, "Settings", typeof(SettingsCommand), "Parameters_32.png", "Edit the Tag 4D settings, load or delete the 4D parameters in the project and generate the IFC mapping file");
                AddButton(ribbon, panelIfc, commandsAssemblyPath, assemblyFolder, "Tag", typeof(TagCommand), "TagElements_32.png", "Tag the selected elements with 4D property values");
                AddButton(ribbon, panelIfc, commandsAssemblyPath, assemblyFolder, "Review", typeof(ReviewCommand), "Review_32.png", "Find the elements that have no value in the chosen 4D parameters and isolate them in a 3D view");
            }
            catch (Exception exception)
            {
                TaskDialog.Show("Spark.Tag4D", $"Spark.Tag4D failed to start: {exception.Message}");
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private static void AddButton(Ribbon ribbon, RibbonPanel panel, string assemblyPath, string assemblyFolder, string title, Type commandType, string imageFileName, string tooltip)
        {
            string imagePath = Path.Combine(assemblyFolder, "Resources", imageFileName);
            ribbon.AddLargeButton(panel, assemblyPath, title, commandType, tooltip, imagePath);
        }

        private void ApplicationViewActivated(object sender, ViewActivatedEventArgs e)
        {
            if (!(sender is UIApplication))
            {
                return;
            }

            UIApplication uiapp = sender as UIApplication;
            // One view model for the whole session: it reads the workbook once and follows selection changes itself.
            if (!(TagView.DataContext is TagViewModel))
            {
                TagView.DataContext = new TagViewModel(uiapp);
            }

            // Created once, here, because the external event of the pane needs a valid Revit API context.
            // (The parameter list is re-read with the Reload button; reading the workbook on every view change is slow.)
            if (!(ReviewView.DataContext is ReviewViewModel))
            {
                ReviewView.DataContext = new ReviewViewModel();
            }

            // Revit restores the panes of the previous session; the add-in panes must start closed. Only done once,
            // so that a pane the user opened stays open when they switch views.
            if (!panesReset)
            {
                panesReset = true;
                HidePane(UIControlledApplication, TagPaneId);
                HidePane(UIControlledApplication, new DockablePaneId(new Guid(Constants.ReviewPaneId)));
            }
        }

        private bool panesReset;

        private void HidePane(UIControlledApplication app, DockablePaneId id)
        {
            try
            {
                var pane = app.GetDockablePane(id);

                if (pane != null && pane.IsShown())
                    pane.Hide();
            }
            catch
            {
            }
        }

    }
}
