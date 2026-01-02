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
                // ⚠️ CONSOLE THEME - Niente viola!
                TimelineStepType.Input => new SolidColorBrush(Color.FromRgb(6, 182, 212)),      // Cyan (era Indigo viola)
                TimelineStepType.Routing => new SolidColorBrush(Color.FromRgb(6, 182, 212)),    // Cyan
                TimelineStepType.Processing => new SolidColorBrush(Color.FromRgb(249, 115, 22)), // Orange
                TimelineStepType.Output => new SolidColorBrush(Color.FromRgb(16, 185, 129)),    // Green
                TimelineStepType.Autonomous => new SolidColorBrush(Color.FromRgb(249, 115, 22)),// Orange (era Purple!)
                TimelineStepType.Dialog => new SolidColorBrush(Color.FromRgb(165, 243, 252)),   // Cyan chiaro
                TimelineStepType.Success => new SolidColorBrush(Color.FromRgb(16, 185, 129)),   // Green
                TimelineStepType.Error => new SolidColorBrush(Color.FromRgb(248, 113, 113)),    // Red
                _ => new SolidColorBrush(Color.FromRgb(156, 163, 175))                           // Gray
            };
        }

        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
