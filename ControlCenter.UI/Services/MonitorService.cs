using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;
using ControlCenter.UI.Models;

namespace ControlCenter.UI.Services;

/// <summary>
/// Servizio per comunicare con Agent.Monitor (porta 5004)
/// </summary>
public class MonitorService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "http://localhost:5004";

    public MonitorService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    /// <summary>
    /// Ottiene lo stato del cluster dal Monitor
    /// </summary>
    public async Task<ClusterStatusResponse?> GetClusterStatusAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/cluster/status");
            
            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                return await response.Content.ReadFromJsonAsync<ClusterStatusResponse>(options);
            }
            
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Ottiene lo stato di salute del cluster dal Monitor
    /// </summary>
    public async Task<ClusterHealthResponse?> GetClusterHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/cluster/health");
            
            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                return await response.Content.ReadFromJsonAsync<ClusterHealthResponse>(options);
            }
            
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Verifica se il Monitor è raggiungibile
    /// </summary>
    public async Task<bool> CheckMonitorHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Risposta dal Monitor per /cluster/status
/// </summary>
public class ClusterStatusResponse
{
    public bool Success { get; set; }
    public DateTime Timestamp { get; set; }
    public List<AgentStatusInfo> Agents { get; set; } = new();
}

/// <summary>
/// Risposta dal Monitor per /cluster/health
/// </summary>
public class ClusterHealthResponse
{
    public bool Success { get; set; }
    public DateTime Timestamp { get; set; }
    public List<AgentHealthInfo> Agents { get; set; } = new();
}

/// <summary>
/// Informazioni di status di un agente
/// </summary>
public class AgentStatusInfo
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public AgentStatusData? Status { get; set; }
}

/// <summary>
/// Dati di status di un agente
/// </summary>
public class AgentStatusData
{
    public string Agent { get; set; } = "";
    public string Uptime { get; set; } = "00:00:00";
    public string Version { get; set; } = "1.0.0";
    public string? LastCommand { get; set; }
    public string? LastTask { get; set; }
}

/// <summary>
/// Informazioni di health di un agente
/// </summary>
public class AgentHealthInfo
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public AgentHealthData? Health { get; set; }
}

/// <summary>
/// Dati di health di un agente
/// </summary>
public class AgentHealthData
{
    public string Status { get; set; } = "";
    public DateTime Timestamp { get; set; }
}
