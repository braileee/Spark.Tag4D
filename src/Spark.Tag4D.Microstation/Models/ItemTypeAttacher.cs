using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.DgnEC;
using Bentley.DgnPlatformNET.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Microstation.Models
{
    public class ItemTypeAttacher
    {
        public ItemTypeAttacher(ModelElementsCollection modelElements)
        {
            ModelElements = modelElements;
        }

        public ModelElementsCollection ModelElements { get; }

        public void Attach(string libraryName, List<ItemType> itemTypes)
        {
            foreach (Element element in ModelElements)
            {
                foreach (ItemType itemType in itemTypes)
                {
                    if (!IsValidTarget(element))
                        continue;

                    Element newElement = element;

                    CustomItemHost customItemHost = new CustomItemHost(newElement, true);

                    if (customItemHost.GetCustomItem(libraryName, itemType.Name) == null)
                    {
                        IDgnECInstance item = customItemHost.ApplyCustomItem(itemType);
                        newElement.ReplaceInModel(element);
                    }
                }
            }
        }

        private bool IsValidTarget(Element el)
        {
            if (el == null) return false;
            if (!el.IsGraphics) return false;

            // Skip annotations, dimensions, etc.
            if (el is DimensionElement) return false;
            if (el is TextElement) return false;

            return true;
        }
    }
}
