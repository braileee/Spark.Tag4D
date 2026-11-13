using Spark.Tag4D.Core.Models;
using System.Windows;
using System.Windows.Controls;

namespace Spark.Tag4D.UI.Controls
{
    public class EditorTemplateSelector : DataTemplateSelector
    {
        public DataTemplate StringTemplate { get; set; }
        public DataTemplate DoubleTemplate { get; set; }
        public DataTemplate IntTemplate { get; set; }
        public DataTemplate BoolTemplate { get; set; }
        public DataTemplate ComboTemplate { get; set; }
        public DataTemplate ReadOnlyTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var field = item as PropertyField;
            if (field == null)
                return base.SelectTemplate(item, container);

            if (field.IsReadOnly && ReadOnlyTemplate != null)
            {
                return ReadOnlyTemplate;
            }

            switch (field.EditorType)
            {
                case EditorType.Bool:
                    return BoolTemplate;

                case EditorType.Double:
                    return DoubleTemplate;

                case EditorType.Int:
                    return IntTemplate;

                case EditorType.String:
                    return StringTemplate;

                case EditorType.Combo:
                    return ComboTemplate;

                default:
                    return StringTemplate;
            }

        }
    }

}
