using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GoogleAdsOptimizer.Converters
{
    /// <summary>
    /// Converts a color name string ("Green", "Red", etc.) to a SolidColorBrush.
    /// </summary>
    public class StringToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string name && !string.IsNullOrWhiteSpace(name))
            {
                try
                {
                    var brush = new BrushConverter().ConvertFromString(name) as Brush;
                    if (brush != null) return brush;
                }
                catch (FormatException)
                {
                    // fall through to default
                }
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
