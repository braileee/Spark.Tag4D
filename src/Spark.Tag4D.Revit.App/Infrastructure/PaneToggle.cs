using Autodesk.Revit.UI;
using Spark.Tag4D.Core;
using System;

namespace Spark.Tag4D.Revit.App.Infrastructure
{
    /// <summary>
    /// Shows or hides one of the add-in panes. The panes share one tab stack, so showing one hides the other first;
    /// otherwise the pane that is already open stays in front and the new one is not visible.
    /// </summary>
    internal static class PaneToggle
    {
        public static void Toggle(UIApplication app, string paneId, string otherPaneId)
        {
            DockablePane pane = app.GetDockablePane(new DockablePaneId(new Guid(paneId)));

            if (pane.IsShown())
            {
                pane.Hide();
                return;
            }

            try
            {
                DockablePane other = app.GetDockablePane(new DockablePaneId(new Guid(otherPaneId)));
                if (other.IsShown())
                {
                    other.Hide();
                }
            }
            catch
            {
            }

            pane.Show();
        }
    }
}
