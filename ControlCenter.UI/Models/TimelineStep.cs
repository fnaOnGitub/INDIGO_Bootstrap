using CommunityToolkit.Mvvm.ComponentModel;

namespace ControlCenter.UI.Models;

/// <summary>
/// Rappresenta uno step nella timeline operativa del cluster
/// </summary>
public partial class TimelineStep : ObservableObject
{
    [ObservableProperty]
    private string _title = "";

    [ObservableProperty]
    private string _description = "";

    [ObservableProperty]
    private DateTime _timestamp = DateTime.Now;

    [ObservableProperty]
    private TimelineStepType _type = TimelineStepType.Info;

    [ObservableProperty]
    private string _icon = "Info";

    [ObservableProperty]
    private bool _isActive = false;

    [ObservableProperty]
    private bool _isCompleted = false;

    public string FormattedTime => Timestamp.ToString("HH:mm:ss");
}

/// <summary>
/// Tipo di step nella timeline
/// </summary>
public enum TimelineStepType
{
    Input,      // Input utente
    Routing,    // Routing / Classificazione
    Processing, // Elaborazione
    Output,     // Output generato
    Autonomous, // Azione autonoma
    Dialog,     // Richiesta dialogo
    Success,    // Completato con successo
    Error,      // Errore
    Info        // Informazione generale
}
