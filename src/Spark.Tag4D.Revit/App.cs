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
using System.Linq;

namespace Spark.Tag4D.Revit
{
    [Transaction(TransactionMode.Manual)]
    public class App : IExternalApplication
    {
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
                string assemblyFolder = AssemblyUtils.GetFolder(typeof(App));

                string[] assemblyFilePathList = Directory.GetFiles(assemblyFolder, "*.dll", SearchOption.TopDirectoryOnly);

                //Ifc manager
                string ifcManagerDllPath = assemblyFilePathList.FirstOrDefault(x => x.Contains($"{nameof(Spark.Tag4D.Revit.App)}.dll"));
                string ifcManagerSettingsImagePath = Path.Combine(assemblyFolder, "Resources", "Settings_32.png");
                PushButton ifcManagerSettingsButton = ribbon.AddLargeButton(panelIfc, ifcManagerDllPath, "Settings", typeof(ParametersSettingsCommand), "Settings", ifcManagerSettingsImagePath);

                string ifcManagerExcelToRevitImagePath = Path.Combine(assemblyFolder, "Resources", "ExcelToRevit_32.png");
                PushButton ifcManagerExcelToRevitButton = ribbon.AddLargeButton(panelIfc, ifcManagerDllPath, "Parameters From Excel", typeof(ParametersExcelToRevitCommand), "Parameters From Excel", ifcManagerExcelToRevitImagePath);

                string generateIfcMappingImagePath = Path.Combine(assemblyFolder, "Resources", "GenerateIfcMapping_32.png");
                PushButton generateIfcMappingButton = ribbon.AddLargeButton(panelIfc, ifcManagerDllPath, "Generate Mapping File", typeof(GenerateIfcMappingCommand), "Generate Mapping File", generateIfcMappingImagePath);

                string tagElementsImagePath = Path.Combine(assemblyFolder, "Resources", "ParametersTagElements_32.png");
                PushButton tagElementsButton = ribbon.AddLargeButton(panelIfc, ifcManagerDllPath, "Tag Elements", typeof(ParametersTagElementsCommand), "Tag Elements", tagElementsImagePath);
            }
            catch (Exception exception)
            {
                TaskDialog.Show("Error", $"Error catched on startup {exception.Message}");
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
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
