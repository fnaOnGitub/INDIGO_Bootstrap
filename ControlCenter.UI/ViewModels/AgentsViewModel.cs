using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlCenter.UI.Models;
using ControlCenter.UI.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ControlCenter.UI.ViewModels;

/// <summary>
/// ViewModel per AgentsPage
/// </summary>
public partial class AgentsViewModel : ObservableObject
{
    private readonly AgentService _agentService;

    public AgentsViewModel()
    {
        _agentService = new AgentService();
        Agents = new ObservableCollection<AgentInfoViewModel>();
        _ = LoadAgentsAsync();
    }

    [ObservableProperty]
    private ObservableCollection<AgentInfoViewModel> _agents;

    [ObservableProperty]
    private bool _isLoading;

    [RelayCommand]
    private void ShowAgentDetails(AgentInfoViewModel agent)
    {
        // Il comando viene chiamato dalla View che gestirà l'apertura del modal
    }

    /// <summary>
    /// Carica la lista degli agenti
    /// </summary>
    private async Task LoadAgentsAsync()
    {
        IsLoading = true;

        try
        {
            var agents = await _agentService.GetAgentsAsync();

            Agents.Clear();
            foreach (var agent in agents)
            {
                Agents.Add(agent);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Aggiorna lo stato degli agenti
    /// </summary>
    [RelayCommand]
    private async Task RefreshAgentsAsync()
    {
        IsLoading = true;

        try
        {
            var updatedAgents = await _agentService.RefreshAgentStatusAsync();

            // Aggiorna la ObservableCollection con i nuovi valori
            foreach (var updatedAgent in updatedAgents)
            {
                var existingAgent = Agents.FirstOrDefault(a => a.Name == updatedAgent.Name);
                if (existingAgent != null)
                {
                    existingAgent.Status = updatedAgent.Status;
                    existingAgent.IsHealthy = updatedAgent.IsHealthy;
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}
