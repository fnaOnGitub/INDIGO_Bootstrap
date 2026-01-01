using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlCenter.UI.Models;
using ControlCenter.UI.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace ControlCenter.UI.ViewModels;

/// <summary>
/// ViewModel per DashboardPage
/// </summary>
public partial class DashboardViewModel : ObservableObject
{
    private readonly BootstrapperClient _client;
    private readonly MonitorService _monitorService;
    private System.Threading.Timer? _statusTimer;

    public DashboardViewModel()
    {
        _client = new BootstrapperClient();
        _monitorService = new MonitorService();
        Logs = new ObservableCollection<string>();
        Agents = new ObservableCollection<AgentStatusItem>();
        
        // Avvia polling automatico ogni 5 secondi
        _statusTimer = new System.Threading.Timer(
            async _ => await RefreshClusterAsync(),
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(5)
        );
    }

    #region Properties

    [ObservableProperty]
    private ClusterStatus? _clusterStatus;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "In attesa...";

    [ObservableProperty]
    private bool _isBootstrapperConnected;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private DateTime _lastUpdated;

    [ObservableProperty]
    private bool _isMonitorConnected;

    public ObservableCollection<string> Logs { get; }
    
    public ObservableCollection<AgentStatusItem> Agents { get; }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task ProvisionAsync()
    {
        await ExecuteCommandAsync(
            "Provisioning",
            async () => await _client.ProvisionAsync()
        );
    }

    [RelayCommand]
    private async Task BuildClusterAsync()
    {
        await ExecuteCommandAsync(
            "Build Cluster",
            async () => await _client.BuildClusterAsync()
        );
    }

    [RelayCommand]
    private async Task GenerateAgentsAsync()
    {
        await ExecuteCommandAsync(
            "Generate Agents",
            async () => await _client.GenerateAgentsAsync()
        );
    }

    [RelayCommand]
    private async Task ConfigureCommunicationAsync()
    {
        await ExecuteCommandAsync(
            "Configure Communication",
            async () => await _client.ConfigureCommunicationAsync()
        );
    }

    [RelayCommand]
    private async Task ValidateAsync()
    {
        await ExecuteCommandAsync(
            "Validate",
            async () => await _client.ValidateAsync()
        );
    }

    [RelayCommand]
    private async Task DeployAsync()
    {
        await ExecuteCommandAsync(
            "Deploy (Full Workflow)",
            async () => await _client.DeployAsync()
        );
    }

    [RelayCommand]
    private async Task RefreshLogsAsync()
    {
        var logs = await _client.GetLogsAsync();
        if (logs != null)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Logs.Clear();
                foreach (var log in logs)
                {
                    Logs.Add(log);
                }
            });
        }
    }

    [RelayCommand]
    private async Task CheckConnectionAsync()
    {
        IsBootstrapperConnected = await _client.CheckHealthAsync();
        StatusMessage = IsBootstrapperConnected
            ? "Connesso al Bootstrapper"
            : "Bootstrapper non raggiungibile";
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await RefreshClusterAsync();
    }

    #endregion

    #region Private Methods

    private async Task RefreshClusterAsync()
    {
        try
        {
            IsLoading = true;

            // Verifica connessione al Monitor
            IsMonitorConnected = await _monitorService.CheckMonitorHealthAsync();

            if (IsMonitorConnected)
            {
                // Ottieni status dal Monitor
                var clusterStatus = await _monitorService.GetClusterStatusAsync();

                if (clusterStatus != null && clusterStatus.Success)
                {
                    // Aggiorna lista agenti
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Agents.Clear();

                        foreach (var agentInfo in clusterStatus.Agents)
                        {
                            var statusData = agentInfo.Status;
                            
                            var agent = new AgentStatusItem
                            {
                                Name = agentInfo.Name,
                                Url = agentInfo.Url,
                                Status = statusData?.Agent ?? "Unknown",
                                Uptime = statusData?.Uptime ?? "00:00:00",
                                LastTask = statusData?.LastTask ?? statusData?.LastCommand ?? "N/A",
                                Version = statusData?.Version ?? "1.0.0",
                                IsHealthy = statusData != null
                            };

                            Agents.Add(agent);
                        }
                    });

                    LastUpdated = DateTime.Now;
                    StatusMessage = $"Cluster attivo - {Agents.Count} agenti";
                }
                else
                {
                    StatusMessage = "Errore: impossibile ottenere lo stato del cluster";
                }
            }
            else
            {
                StatusMessage = "Monitor non raggiungibile";
            }

            // Mantieni anche il polling del Bootstrapper per i log
            await RefreshStatusAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Errore aggiornamento: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshStatusAsync()
    {
        // Aggiorna stato cluster dal Bootstrapper (legacy)
        var status = await _client.GetStatusAsync();
        if (status != null)
        {
            ClusterStatus = status;
            IsBusy = status.IsBusy;
        }

        // Verifica connessione Bootstrapper
        IsBootstrapperConnected = await _client.CheckHealthAsync();

        // Aggiorna log
        await RefreshLogsAsync();
    }

    private async Task ExecuteCommandAsync(string commandName, Func<Task<CommandResponse?>> commandAction)
    {
        try
        {
            IsBusy = true;
            StatusMessage = $"Esecuzione {commandName}...";

            var response = await commandAction();

            if (response?.Success == true)
            {
                StatusMessage = $"{commandName} completato con successo";
            }
            else
            {
                StatusMessage = $"{commandName} fallito: {response?.Message ?? "Errore sconosciuto"}";
            }

            // Aggiorna stato e log
            await RefreshStatusAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Errore {commandName}: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion

    public void Dispose()
    {
        _statusTimer?.Dispose();
    }
}
