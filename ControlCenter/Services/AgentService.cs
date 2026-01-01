using ControlCenter.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ControlCenter.Core.Services;

/// <summary>
/// Servizio per la gestione degli agenti del cluster
/// </summary>
public class AgentService
{
    private readonly Random _random;
    private List<AgentInfoViewModel> _cachedAgents;

    public AgentService()
    {
        _random = new Random();
        _cachedAgents = new List<AgentInfoViewModel>();
        InitializeAgents();
    }

    /// <summary>
    /// Ottiene la lista degli agenti configurati
    /// </summary>
    public async Task<List<AgentInfoViewModel>> GetAgentsAsync()
    {
        // Simula chiamata asincrona
        await Task.Delay(100);

        return new List<AgentInfoViewModel>(_cachedAgents);
    }

    /// <summary>
    /// Aggiorna lo stato degli agenti (simulato)
    /// </summary>
    public async Task<List<AgentInfoViewModel>> RefreshAgentStatusAsync()
    {
        // Simula chiamata asincrona per aggiornare lo stato
        await Task.Delay(300);

        foreach (var agent in _cachedAgents)
        {
            // Simula stati casuali
            var statusRoll = _random.Next(0, 100);

            if (statusRoll < 70)
            {
                agent.Status = "Running";
                agent.IsHealthy = true;
            }
            else if (statusRoll < 90)
            {
                agent.Status = "Starting";
                agent.IsHealthy = false;
            }
            else
            {
                agent.Status = "Stopped";
                agent.IsHealthy = false;
            }
        }

        return new List<AgentInfoViewModel>(_cachedAgents);
    }

    /// <summary>
    /// Inizializza la lista degli agenti di esempio
    /// </summary>
    private void InitializeAgents()
    {
        _cachedAgents = new List<AgentInfoViewModel>
        {
            new AgentInfoViewModel
            {
                Name = "agent-orchestrator",
                Type = "orchestrator",
                Port = 5001,
                Status = "Unknown",
                IsHealthy = false
            },
            new AgentInfoViewModel
            {
                Name = "agent-worker-01",
                Type = "worker",
                Port = 5002,
                Status = "Unknown",
                IsHealthy = false
            },
            new AgentInfoViewModel
            {
                Name = "agent-worker-02",
                Type = "worker",
                Port = 5003,
                Status = "Unknown",
                IsHealthy = false
            },
            new AgentInfoViewModel
            {
                Name = "agent-monitor",
                Type = "monitor",
                Port = 5004,
                Status = "Unknown",
                IsHealthy = false
            }
        };
    }
}
