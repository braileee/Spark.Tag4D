using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Spark.Tag4D.UI.Controls
{
    /// <summary>The search box used on every page: bind <see cref="Text"/> to the filter text of the page.</summary>
    public partial class SearchBox : UserControl
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(SearchBox),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty HintProperty = DependencyProperty.Register(
            nameof(Hint), typeof(string), typeof(SearchBox), new PropertyMetadata("Search"));

        public SearchBox()
        {
            InitializeComponent();
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>Shown while the box is empty, e.g. "Find a picklist or value".</summary>
        public string Hint
        {
            get => (string)GetValue(HintProperty);
            set => SetValue(HintProperty, value);
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            Text = string.Empty;
            Box.Focus();
        }
    }

    /// <summary>Visible for a non-empty text, collapsed for an empty one.</summary>
    public sealed class TextToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
