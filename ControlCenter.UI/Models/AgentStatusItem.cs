using CommunityToolkit.Mvvm.ComponentModel;

namespace ControlCenter.UI.Models;

/// <summary>
/// Rappresenta lo stato di un agente nel cluster
/// </summary>
public partial class AgentStatusItem : ObservableObject
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _url = "";

    [ObservableProperty]
    private string _status = "Unknown";

    [ObservableProperty]
    private string _uptime = "00:00:00";

    [ObservableProperty]
    private string _lastTask = "N/A";

    [ObservableProperty]
    private bool _isHealthy;

    [ObservableProperty]
    private string _version = "1.0.0";
}
