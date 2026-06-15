using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SystemManager.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ViewModels.OperationStatus status)
            {
                return status switch
                {
                    ViewModels.OperationStatus.Ready => new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                    ViewModels.OperationStatus.Processing => new SolidColorBrush(Color.FromRgb(255, 165, 0)),
                    ViewModels.OperationStatus.Completed => new SolidColorBrush(Color.FromRgb(16, 124, 16)),
                    ViewModels.OperationStatus.Error => new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                    ViewModels.OperationStatus.Warning => new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                    _ => new SolidColorBrush(Color.FromRgb(0, 122, 204))
                };
            }
            return new SolidColorBrush(Color.FromRgb(0, 122, 204));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}