using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Spark.Tag4D.Core;
using Spark.Tag4D.Core.Utils;
using Spark.Tag4D.Revit.Core.Models;
using Spark.Tag4D.Revit.App.Commands;
using Spark.Tag4D.Revit.App.ViewModels;
using Spark.Tag4D.Revit.App.Views;
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

        public ParametersTagElementsView ParametersTagElementsView { get; private set; }
        public DockablePaneId IfcTagElementsPaneId { get; private set; }
        public UIControlledApplication UIControlledApplication { get; private set; }

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                UIControlledApplication = application;
                Ribbon ribbon = new Ribbon(application);
                ribbon.AddTab(Constants.TabName);
                RibbonPanel panelIfc = ribbon.AddPanel(Constants.TabName, Constants.PanelNameIfc);

                ParametersTagElementsView = new ParametersTagElementsView();
                IfcTagElementsPaneId = new DockablePaneId(new Guid(Constants.ParametersTagElementsPaneId));
                application.RegisterDockablePane(IfcTagElementsPaneId, "IFC Tag Elements", ParametersTagElementsView);
                HidePane(application, IfcTagElementsPaneId);

                application.ViewActivated -= ApplicationViewActivated;
                application.ViewActivated += ApplicationViewActivated;
                string assemblyFolder = AssemblyUtils.GetFolder(typeof(Tag4DApplication));
                string commandsAssemblyPath = typeof(ParametersCommand).Assembly.Location;

                AddButton(ribbon, panelIfc, commandsAssemblyPath, assemblyFolder, "Parameters", typeof(ParametersCommand), "Parameters_32.png", "Load or delete the IFC parameters in the project, edit the settings and generate the IFC mapping file");
                AddButton(ribbon, panelIfc, commandsAssemblyPath, assemblyFolder, "Tag Elements", typeof(ParametersTagElementsCommand), "TagElements_32.png", "Tag the selected elements with IFC properties");
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
            ParametersTagElementsViewModel parametersTagElementsViewModel = new ParametersTagElementsViewModel(uiapp);
            ParametersTagElementsView.DataContext = parametersTagElementsViewModel;

            HidePane(UIControlledApplication, IfcTagElementsPaneId);
        }

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
