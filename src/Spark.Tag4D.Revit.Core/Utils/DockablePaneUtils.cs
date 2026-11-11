using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Revit.Core.Utils
{
    public static class DockablePaneUtils
    {
        public static void HidePane(UIControlledApplication app, DockablePaneId id)
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
