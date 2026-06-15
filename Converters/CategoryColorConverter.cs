using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SystemManager.Converters
{
    public class CategoryColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() switch
            {
                "Registry" => new SolidColorBrush(Color.FromRgb(0x0E, 0x63, 0x9C)),
                "File" => new SolidColorBrush(Color.FromRgb(0x0E, 0x7A, 0x3A)),
                "Process" => new SolidColorBrush(Color.FromRgb(0x8B, 0x5C, 0xF6)),
                "System" => new SolidColorBrush(Color.FromRgb(0xC5, 0x0F, 0x1F)),
                "Console" => new SolidColorBrush(Color.FromRgb(0xFF, 0xA5, 0x00)),
                "Navigation" => new SolidColorBrush(Color.FromRgb(0x60, 0x60, 0x60)),
                "Cleanup" => new SolidColorBrush(Color.FromRgb(0x00, 0x96, 0x88)),
                _ => new SolidColorBrush(Color.FromRgb(0x60, 0x60, 0x60))
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}