using Spark.Tag4D.Core.Models;
using Spark.Tag4D.Settings.ViewModels;
using Spark.Tag4D.Settings.ViewModels.Support;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Spark.Tag4D.Settings.Views
{
    /// <summary>
    /// Composed properties. Segments are reordered (also between formulas) by dragging their handle; a property from
    /// the palette is dropped onto a formula to add it as a new segment.
    /// </summary>
    public partial class ComposedView : UserControl
    {
        private const string SegmentFormat = "Spark.Tag4D.FormulaSegment";

        public static readonly DependencyProperty DropSideProperty = DependencyProperty.RegisterAttached(
            "DropSide", typeof(string), typeof(ComposedView), new PropertyMetadata(string.Empty));

        public static string GetDropSide(DependencyObject element) => (string)element.GetValue(DropSideProperty);

        public static void SetDropSide(DependencyObject element, string value) => element.SetValue(DropSideProperty, value);

        private Point? _dragStart;

        public ComposedView()
        {
            InitializeComponent();
        }

        private ComposedViewModel ViewModel => DataContext as ComposedViewModel;

        // ---- start a drag -------------------------------------------------------------------------------------

        private void Grip_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStart = e.GetPosition(null);
        }

        private void Grip_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!IsDragGesture(e))
            {
                return;
            }

            var element = (FrameworkElement)sender;
            if (element.DataContext is FormulaSegment segment)
            {
                DragDrop.DoDragDrop(element, new DataObject(SegmentFormat, segment), DragDropEffects.Move);
            }
        }

        private void Chip_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStart = e.GetPosition(null);
        }

        private void Chip_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!IsDragGesture(e))
            {
                return;
            }

            var element = (FrameworkElement)sender;
            if (element.DataContext is PropertyItem property)
            {
                DragDrop.DoDragDrop(element, new DataObject(DataFormats.StringFormat, property.PropertyName), DragDropEffects.Copy);
            }
        }

        private bool IsDragGesture(MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _dragStart == null)
            {
                return false;
            }

            Vector distance = e.GetPosition(null) - _dragStart.Value;
            if (Math.Abs(distance.X) < SystemParameters.MinimumHorizontalDragDistance
                && Math.Abs(distance.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return false;
            }

            _dragStart = null;
            return true;
        }

        // ---- drop onto a segment: before or after it, depending on the half the pointer is over -----------------

        private void Segment_PreviewDragOver(object sender, DragEventArgs e)
        {
            var card = (FrameworkElement)sender;
            e.Handled = true;

            if (!Accepts(e))
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            e.Effects = e.Data.GetDataPresent(SegmentFormat) ? DragDropEffects.Move : DragDropEffects.Copy;
            SetDropSide(card, e.GetPosition(card).X < card.ActualWidth / 2 ? "Left" : "Right");
        }

        private void Segment_PreviewDragLeave(object sender, DragEventArgs e)
        {
            SetDropSide((DependencyObject)sender, string.Empty);
        }

        private void Segment_PreviewDrop(object sender, DragEventArgs e)
        {
            var card = (FrameworkElement)sender;
            e.Handled = true;

            string side = GetDropSide(card);
            SetDropSide(card, string.Empty);

            if (!(card.DataContext is FormulaSegment target) || target.Parent == null)
            {
                return;
            }

            int index = target.Parent.Segments.IndexOf(target) + (side == "Right" ? 1 : 0);
            Drop(e, target.Parent, index);
        }

        // ---- drop on the free space of a formula: append ----------------------------------------------------------

        private void Row_DragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            e.Effects = Accepts(e)
                ? (e.Data.GetDataPresent(SegmentFormat) ? DragDropEffects.Move : DragDropEffects.Copy)
                : DragDropEffects.None;
        }

        private void Row_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;

            if (((FrameworkElement)sender).DataContext is ComposedProperty target)
            {
                Drop(e, target, target.Segments.Count);
            }
        }

        // ---- shared -------------------------------------------------------------------------------------------

        private static bool Accepts(DragEventArgs e)
        {
            return e.Data.GetDataPresent(SegmentFormat) || e.Data.GetDataPresent(DataFormats.StringFormat);
        }

        private void Drop(DragEventArgs e, ComposedProperty target, int index)
        {
            if (ViewModel == null)
            {
                return;
            }

            if (e.Data.GetData(SegmentFormat) is FormulaSegment segment)
            {
                ViewModel.MoveSegment(segment, target, index);
            }
            else if (e.Data.GetData(DataFormats.StringFormat) is string propertyName)
            {
                ViewModel.InsertProperty(propertyName, target, index);
            }
        }
    }
}
