using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ControlCenter.UI.Models;

namespace ControlCenter.UI.Converters;

/// <summary>
/// Converte TimelineStepType in colore
/// </summary>
public class StepTypeToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TimelineStepType type)
        {
            return type switch
            {
                TimelineStepType.Input => new SolidColorBrush(Color.FromRgb(75, 0, 130)),      // Indigo
                TimelineStepType.Routing => new SolidColorBrush(Color.FromRgb(0, 188, 212)),   // Cyan
                TimelineStepType.Processing => new SolidColorBrush(Color.FromRgb(255, 152, 0)), // Orange
                TimelineStepType.Output => new SolidColorBrush(Color.FromRgb(76, 175, 80)),    // Green
                TimelineStepType.Autonomous => new SolidColorBrush(Color.FromRgb(156, 39, 176)),// Purple-ish
                TimelineStepType.Dialog => new SolidColorBrush(Color.FromRgb(33, 150, 243)),   // Blue
                TimelineStepType.Success => new SolidColorBrush(Color.FromRgb(76, 175, 80)),   // Green
                TimelineStepType.Error => new SolidColorBrush(Color.FromRgb(244, 67, 54)),     // Red
                _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))                          // Gray
            };
        }

        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
