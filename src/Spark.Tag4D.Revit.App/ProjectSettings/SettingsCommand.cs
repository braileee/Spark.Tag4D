using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Windows.Interop;

namespace Spark.Tag4D.Revit.App.ProjectSettings
{
    /// <summary>Opens the (modeless) Parameters window: settings, load / delete parameters, IFC mapping file.</summary>
    [Transaction(TransactionMode.Manual)]
    public class SettingsCommand : IExternalCommand
    {
        private static ProjectSettingsView window;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (window != null)
                {
                    window.Activate();
                    return Result.Succeeded;
                }

                // The external event must be created here, inside a valid Revit API context.
                var viewModel = new ProjectSettingsViewModel(new ParametersOperationEventHandler());
                window = new ProjectSettingsView { DataContext = viewModel };
                new WindowInteropHelper(window) { Owner = commandData.Application.MainWindowHandle };
                window.Closed += (sender, args) => window = null;
                window.Show();
            }
            catch (Exception exception)
            {
                window = null;
                TaskDialog.Show("Spark.Tag4D", $"The Parameters window could not be opened: {exception.Message}");
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }
}
