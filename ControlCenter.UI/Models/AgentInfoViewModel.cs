using CommunityToolkit.Mvvm.ComponentModel;

namespace ControlCenter.UI.Models;

/// <summary>
/// Informazioni agente
/// </summary>
public partial class AgentInfoViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _type = "";

    [ObservableProperty]
    private int _port;

    [ObservableProperty]
    private string _status = "Unknown";

    [ObservableProperty]
    private bool _isHealthy;
}
