using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EgeTracker.Converters
{
    /// <summary>
    /// Показывает элемент, если строка не пустая и не null.
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool hasValue = !string.IsNullOrWhiteSpace(value as string);
            if (parameter is string s && s.Equals("invert", StringComparison.OrdinalIgnoreCase))
                hasValue = !hasValue;
            return hasValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}