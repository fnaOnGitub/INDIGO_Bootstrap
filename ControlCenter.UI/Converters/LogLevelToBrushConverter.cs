using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ControlCenter.UI.Services;

namespace ControlCenter.UI.Converters;

public class LogLevelToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is LogLevel level)
        {
            return level switch
            {
                LogLevel.Info => new SolidColorBrush(Color.FromRgb(205, 214, 244)), // #CDD6F4
                LogLevel.Warning => new SolidColorBrush(Color.FromRgb(249, 226, 175)), // #F9E2AF
                LogLevel.Error => new SolidColorBrush(Color.FromRgb(243, 139, 168)), // #F38BA8
                _ => Brushes.White
            };
        }
        return Brushes.White;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
