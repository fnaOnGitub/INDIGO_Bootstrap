using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlCenter.Core.Models;
using ControlCenter.Core.Services;
using System.Collections.ObjectModel;

namespace ControlCenter.Core.ViewModels;

/// <summary>
/// ViewModel per DashboardPage
/// </summary>
public partial class DashboardViewModel : ObservableObject
{
    private readonly BootstrapperClient _client;
    private System.Threading.Timer? _statusTimer;

    public DashboardViewModel()
    {
        _client = new BootstrapperClient();
        Logs = new ObservableCollection<string>();
        
        // Avvia polling automatico ogni 2 secondi
        _statusTimer = new System.Threading.Timer(
            async _ => await RefreshStatusAsync(),
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(2)
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

    public ObservableCollection<string> Logs { get; }

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
            Logs.Clear();
            foreach (var log in logs)
            {
                Logs.Add(log);
            }
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

    #endregion

    #region Private Methods

    private async Task RefreshStatusAsync()
    {
        // Aggiorna stato cluster
        var status = await _client.GetStatusAsync();
        if (status != null)
        {
            ClusterStatus = status;
            IsBusy = status.IsBusy;

            // Aggiorna messaggio stato
            if (status.IsProvisioning)
                StatusMessage = "Provisioning in corso...";
            else if (status.IsBuildingCluster)
                StatusMessage = "Build cluster in corso...";
            else if (status.IsGeneratingAgents)
                StatusMessage = "Generazione agenti in corso...";
            else if (status.IsConfiguringCommunication)
                StatusMessage = "Configurazione comunicazione in corso...";
            else if (status.IsValidating)
                StatusMessage = "Validazione in corso...";
            else
                StatusMessage = "Pronto";
        }

        // Verifica connessione
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
