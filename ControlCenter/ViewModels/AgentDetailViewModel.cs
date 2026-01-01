using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlCenter.Core.Models;

namespace ControlCenter.Core.ViewModels;

/// <summary>
/// ViewModel per AgentDetailModal
/// </summary>
public partial class AgentDetailViewModel : ObservableObject
{
    public AgentDetailViewModel(AgentInfoViewModel agent)
    {
        Agent = agent;
    }

    [ObservableProperty]
    private AgentInfoViewModel _agent = new();

    [ObservableProperty]
    private string _health = "Checking...";

    [ObservableProperty]
    private string _metadata = "";

    [ObservableProperty]
    private string _agentLogs = "";

    [ObservableProperty]
    private bool _isTestingAgent;

    [RelayCommand]
    private async Task TestAgentAsync()
    {
        IsTestingAgent = true;
        
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            
            // Test health
            var healthUrl = $"http://localhost:{Agent.Port}/health";
            var healthResponse = await httpClient.GetAsync(healthUrl);
            
            if (healthResponse.IsSuccessStatusCode)
            {
                Health = $"✅ Healthy - Status: {healthResponse.StatusCode}";
                var healthContent = await healthResponse.Content.ReadAsStringAsync();
                AgentLogs += $"\n[Health] {healthContent}";
            }
            else
            {
                Health = $"❌ Unhealthy - Status: {healthResponse.StatusCode}";
            }

            // Test metadata
            var metadataUrl = $"http://localhost:{Agent.Port}/metadata";
            var metadataResponse = await httpClient.GetAsync(metadataUrl);
            
            if (metadataResponse.IsSuccessStatusCode)
            {
                Metadata = await metadataResponse.Content.ReadAsStringAsync();
            }
        }
        catch (Exception ex)
        {
            Health = $"❌ Error: {ex.Message}";
            AgentLogs += $"\n[Error] {ex.Message}";
        }
        finally
        {
            IsTestingAgent = false;
        }
    }
}
