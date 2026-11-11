using System;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace Spark.Tag4D.Revit.Core.Models
{
    public class Ribbon
    {
        public Ribbon(UIControlledApplication application)
        {
            Application = application;
        }

        public UIControlledApplication Application { get; }

        public void AddTab(string tabName)
        {
            Application.CreateRibbonTab(tabName);
        }

        public RibbonPanel AddPanel(string tabName, string panelName)
        {
            if (Application.GetRibbonPanels(tabName).Any(x => x.Name == panelName))
            {
                return Application.GetRibbonPanels(tabName).FirstOrDefault(item => item.Name == panelName);
            }

            RibbonPanel ribbonPanel = Application.CreateRibbonPanel(tabName, panelName);
            return ribbonPanel;
        }

        public PushButton AddLargeButton(RibbonPanel ribbonPanel, string dllFilePath, string buttonName, Type commandType, string tooltip, string imagePath)
        {
            PushButtonData buttonData = new PushButtonData(buttonName, buttonName, dllFilePath, commandType.FullName);
            PushButton button = ribbonPanel.AddItem(buttonData) as PushButton;
            button.ToolTip = tooltip;

            if (File.Exists(imagePath))
            {
                button.LargeImage = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
            }

            return button;
        }
    }
}
