using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FaceDiff.Converters
{
    public class ColorToBrushConverter : IValueConverter
    {
        public double Opacity { get; set; } = 1.0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                var brush = new SolidColorBrush(color);
                if (parameter is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double op))
                    brush.Opacity = op;
                return brush;
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
