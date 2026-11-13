using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Spark.Tag4D.UI.Controls
{
    /// <summary>
    /// A pick list: the whole box opens the drop-down, which has a search box on top that narrows the values down while
    /// typing. Values can only be picked, never typed in. The mouse wheel does not change the value of the closed box.
    /// </summary>
    public partial class SearchableComboBox : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource), typeof(IEnumerable), typeof(SearchableComboBox), new PropertyMetadata(null, OnItemsSourceChanged));

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            nameof(SelectedItem), typeof(object), typeof(SearchableComboBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

        public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
            nameof(IsOpen), typeof(bool), typeof(SearchableComboBox), new PropertyMetadata(false, OnIsOpenChanged));

        public SearchableComboBox()
        {
            InitializeComponent();

            PreviewMouseWheel += (s, e) =>
            {
                if (!IsOpen)
                {
                    // Let the surrounding scroll viewer scroll instead of changing the value.
                    e.Handled = true;
                    var args = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta) { RoutedEvent = MouseWheelEvent, Source = this };
                    (Parent as UIElement)?.RaiseEvent(args);
                }
            };
        }

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var box = (SearchableComboBox)d;

            if (e.OldValue is INotifyCollectionChanged oldCollection)
            {
                oldCollection.CollectionChanged -= box.OnItemsChanged;
            }

            if (e.NewValue is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += box.OnItemsChanged;
            }
        }

        private void OnItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (IsOpen)
            {
                Filter();
            }
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SearchableComboBox)d).Display.Text = e.NewValue?.ToString() ?? string.Empty;
        }

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var box = (SearchableComboBox)d;

            if ((bool)e.NewValue)
            {
                box.SearchBox.Text = string.Empty;
                box.Filter();
                box.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                {
                    box.SearchBox.Focus();
                    Keyboard.Focus(box.SearchBox);
                }));
            }
        }

        private void Filter()
        {
            string text = SearchBox.Text.Trim();
            var items = (ItemsSource ?? Enumerable.Empty<object>()).Cast<object>()
                .Where(item => text.Length == 0 || (item?.ToString() ?? string.Empty).IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            List.ItemsSource = items;
            List.SelectedItem = items.FirstOrDefault(item => Equals(item, SelectedItem));
            NoMatches.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsOpen)
            {
                Filter();
            }
        }

        private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    IsOpen = false;
                    e.Handled = true;
                    break;
                case Key.Down:
                    List.SelectedIndex = Math.Min(List.Items.Count - 1, List.SelectedIndex + 1);
                    List.ScrollIntoView(List.SelectedItem);
                    e.Handled = true;
                    break;
                case Key.Up:
                    List.SelectedIndex = Math.Max(0, List.SelectedIndex - 1);
                    List.ScrollIntoView(List.SelectedItem);
                    e.Handled = true;
                    break;
                case Key.Enter:
                    // Enter picks the highlighted value, or the only one left.
                    object item = List.SelectedItem ?? (List.Items.Count == 1 ? List.Items[0] : null);
                    if (item != null)
                    {
                        Pick(item);
                    }

                    e.Handled = true;
                    break;
            }
        }

        private void List_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var element = e.OriginalSource as DependencyObject;
            while (element != null && !(element is ListBoxItem))
            {
                element = VisualTreeHelper.GetParent(element);
            }

            if (element is ListBoxItem container)
            {
                Pick(container.DataContext);
            }
        }

        private void Pick(object item)
        {
            IsOpen = false;
            SelectedItem = item;
        }
    }
}
