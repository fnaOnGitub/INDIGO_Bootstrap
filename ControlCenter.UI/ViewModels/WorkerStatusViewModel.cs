using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using ControlCenter.UI.Services;

namespace ControlCenter.UI.ViewModels;

/// <summary>
/// ViewModel per visualizzare lo stato di un worker
/// </summary>
public class WorkerStatusViewModel : INotifyPropertyChanged
{
    private AgentStatus _status = AgentStatus.NotStarted;
    private AgentDiagnostics? _diagnostics;

    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int Port { get; set; }

    public AgentStatus Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(StatusBadgeBackground));
                OnPropertyChanged(nameof(StatusBadgeForeground));
                OnPropertyChanged(nameof(IsRunning));
            }
        }
    }

    public AgentDiagnostics? Diagnostics
    {
        get => _diagnostics;
        set
        {
            _diagnostics = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DiagnosticInfo));
        }
    }

    public bool IsRunning => Status == AgentStatus.Running;

    public string StatusText => Status switch
    {
        AgentStatus.NotStarted => "NON AVVIATO",
        AgentStatus.Starting => "IN AVVIO...",
        AgentStatus.Running => "ATTIVO",
        AgentStatus.Crashed => "CRASHED",
        _ => "SCONOSCIUTO"
    };

    public SolidColorBrush StatusColor => Status switch
    {
        AgentStatus.Running => new SolidColorBrush(Color.FromRgb(16, 185, 129)), // Verde
        AgentStatus.Starting => new SolidColorBrush(Color.FromRgb(245, 158, 11)), // Arancione
        AgentStatus.Crashed => new SolidColorBrush(Color.FromRgb(239, 68, 68)), // Rosso
        _ => new SolidColorBrush(Color.FromRgb(156, 163, 175)) // Grigio
    };

    public SolidColorBrush StatusBadgeBackground => Status switch
    {
        AgentStatus.Running => new SolidColorBrush(Color.FromRgb(220, 252, 231)), // Verde chiaro
        AgentStatus.Starting => new SolidColorBrush(Color.FromRgb(254, 243, 199)), // Arancione chiaro
        AgentStatus.Crashed => new SolidColorBrush(Color.FromRgb(254, 226, 226)), // Rosso chiaro
        _ => new SolidColorBrush(Color.FromRgb(243, 244, 246)) // Grigio chiaro
    };

    public SolidColorBrush StatusBadgeForeground => Status switch
    {
        AgentStatus.Running => new SolidColorBrush(Color.FromRgb(21, 128, 61)), // Verde scuro
        AgentStatus.Starting => new SolidColorBrush(Color.FromRgb(180, 83, 9)), // Arancione scuro
        AgentStatus.Crashed => new SolidColorBrush(Color.FromRgb(153, 27, 27)), // Rosso scuro
        _ => new SolidColorBrush(Color.FromRgb(75, 85, 99)) // Grigio scuro
    };

    public string DiagnosticInfo
    {
        get
        {
            if (Diagnostics == null) return "Nessuna diagnostica disponibile";

            var parts = new System.Collections.Generic.List<string>();

            if (Diagnostics.LastOutputTime.HasValue)
            {
                var elapsed = DateTime.Now - Diagnostics.LastOutputTime.Value;
                parts.Add($"Ultimo output: {elapsed.TotalSeconds:F0}s fa");
            }

            parts.Add($"Log ricevuti: {Diagnostics.OutputLinesReceived}");
            parts.Add($"Errori: {Diagnostics.ErrorLinesReceived}");

            if (!string.IsNullOrEmpty(Diagnostics.LastError))
            {
                parts.Add($"Ultimo errore: {Diagnostics.LastError.Substring(0, Math.Min(50, Diagnostics.LastError.Length))}...");
            }

            return string.Join(" | ", parts);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
