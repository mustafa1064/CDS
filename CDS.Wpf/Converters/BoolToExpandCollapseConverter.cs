using System;
using System.Globalization;
using System.Windows.Data;

namespace CDS.Wpf.Converters
{
    public class BoolToExpandCollapseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool expanded = (bool)value;
            return expanded ? "▼" : "▶";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}