using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using FaceDiff.Models;

namespace FaceDiff.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DetectionStatus status)
            {
                switch (status)
                {
                    case DetectionStatus.AutoDetected:
                        return new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    case DetectionStatus.NoFaceFound:
                        return new SolidColorBrush(Color.FromRgb(244, 67, 54));
                    case DetectionStatus.ManualOverride:
                        return new SolidColorBrush(Color.FromRgb(255, 193, 7));
                    default:
                        return new SolidColorBrush(Color.FromRgb(158, 158, 158));
                }
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
