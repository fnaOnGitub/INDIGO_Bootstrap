using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ControlCenter.UI.Converters;

/// <summary>
/// Converte bool a colore (true = verde, false = rosso)
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue 
                ? new SolidColorBrush(Color.FromRgb(76, 175, 80))  // Verde
                : new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Rosso
        }

        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
