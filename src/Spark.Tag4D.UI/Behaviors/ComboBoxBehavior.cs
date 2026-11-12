using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Spark.Tag4D.UI.Behaviors
{
    /// <summary>
    /// Stops the mouse wheel from changing the value of a closed <see cref="ComboBox"/>: the wheel scrolls the
    /// surrounding panel instead, so a value cannot be changed by accident. An open drop-down scrolls normally.
    /// </summary>
    public static class ComboBoxBehavior
    {
        public static readonly DependencyProperty LockWheelProperty = DependencyProperty.RegisterAttached(
            "LockWheel", typeof(bool), typeof(ComboBoxBehavior), new PropertyMetadata(false, OnLockWheelChanged));

        public static bool GetLockWheel(DependencyObject element) => (bool)element.GetValue(LockWheelProperty);

        public static void SetLockWheel(DependencyObject element, bool value) => element.SetValue(LockWheelProperty, value);

        private static void OnLockWheelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(sender is ComboBox comboBox))
            {
                return;
            }

            comboBox.PreviewMouseWheel -= OnPreviewMouseWheel;

            if ((bool)e.NewValue)
            {
                comboBox.PreviewMouseWheel += OnPreviewMouseWheel;
            }
        }

        private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;

            if (comboBox.IsDropDownOpen)
            {
                return;
            }

            // Swallow the wheel for the combo box and hand it to whatever scrolls behind it.
            e.Handled = true;

            if (VisualTreeHelper.GetParent(comboBox) is UIElement parent)
            {
                parent.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = UIElement.MouseWheelEvent,
                    Source = comboBox,
                });
            }
        }
    }
}
