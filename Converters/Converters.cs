using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SystemManager.ViewModels;

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

    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : value;
    }

    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is OperationStatus status)
            {
                return status switch
                {
                    OperationStatus.Ready => new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                    OperationStatus.Processing => new SolidColorBrush(Color.FromRgb(255, 165, 0)),
                    OperationStatus.Completed => new SolidColorBrush(Color.FromRgb(16, 124, 16)),
                    OperationStatus.Error => new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                    OperationStatus.Warning => new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                    _ => new SolidColorBrush(Color.FromRgb(0, 122, 204))
                };
            }
            return new SolidColorBrush(Color.FromRgb(0, 122, 204));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}