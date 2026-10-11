using System;
using System.Globalization;
using System.Windows.Data;

namespace EgeTracker.Converters
{
    /// <summary>
    /// Преобразует значение 0..100 в угол 0..360 для круговой диаграммы.
    /// </summary>
    public class PercentToAngleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double percent = 0;
            if (value is double d) percent = d;
            else if (value is int i) percent = i;
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            return percent * 3.6;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}