using ControlCenter.UI.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ControlCenter.UI.Services;

/// <summary>
/// Servizio per la gestione degli agenti del cluster
/// </summary>
public class AgentService
{
    private readonly Random _random;
    private readonly BootstrapperClient _client;
    private List<AgentInfoViewModel> _cachedAgents;

    public AgentService()
    {
        _random = new Random();
        _client = new BootstrapperClient();
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
    /// Testa un agente specifico tramite il Bootstrapper
    /// </summary>
    /// <param name="agentName">Nome dell'agente da testare</param>
    /// <returns>Risultato del test con messaggio</returns>
    public async Task<TestAgentResponse> TestAgentAsync(string agentName)
    {
        return await _client.TestAgentAsync(agentName);
    }

    /// <summary>
    /// Invia un task all'Orchestrator tramite il Bootstrapper
    /// </summary>
    /// <param name="agentName">Nome dell'agente (orchestrator)</param>
    /// <param name="task">Nome del task da eseguire</param>
    /// <param name="payload">Payload del task</param>
    /// <returns>Risultato del dispatch</returns>
    public async Task<DispatchResponse> DispatchTaskAsync(string agentName, string task, string payload)
    {
        return await _client.DispatchTaskAsync(agentName, task, payload);
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
