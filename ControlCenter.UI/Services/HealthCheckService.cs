using System.Net.Http;

namespace ControlCenter.UI.Services;

/// <summary>
/// Servizio per verificare lo stato di salute dell'Orchestrator
/// </summary>
public class HealthCheckService
{
    private readonly HttpClient _httpClient;
    private readonly List<int> _candidatePorts = new() { 5001, 5101, 7001 };
    
    public int? ActivePort { get; private set; }
    public string ActiveUrl => ActivePort.HasValue ? $"http://localhost:{ActivePort}" : "";
    public bool IsOrchestratorOnline { get; private set; }
    public TimeSpan LastResponseTime { get; private set; }

    public HealthCheckService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
    }

    /// <summary>
    /// Verifica se l'Orchestrator è attivo su una delle porte candidate
    /// </summary>
    public async Task<(bool IsOnline, int? Port, string Message)> CheckOrchestratorAsync()
    {
        foreach (var port in _candidatePorts)
        {
            var result = await CheckPortAsync(port);
            if (result.IsOnline)
            {
                ActivePort = port;
                IsOrchestratorOnline = true;
                return (true, port, $"Orchestrator attivo su porta {port}");
            }
        }

        ActivePort = null;
        IsOrchestratorOnline = false;
        return (false, null, "Orchestrator non trovato su nessuna porta candidata");
    }

    /// <summary>
    /// Verifica se l'Orchestrator risponde su una porta specifica
    /// </summary>
    public async Task<(bool IsOnline, TimeSpan ResponseTime)> CheckPortAsync(int port)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var url = $"http://localhost:{port}/health";
            
            var response = await _httpClient.GetAsync(url);
            var responseTime = DateTime.UtcNow - startTime;
            
            if (response.IsSuccessStatusCode)
            {
                LastResponseTime = responseTime;
                return (true, responseTime);
            }

            return (false, TimeSpan.Zero);
        }
        catch
        {
            return (false, TimeSpan.Zero);
        }
    }

    /// <summary>
    /// Verifica periodica dello stato Orchestrator
    /// </summary>
    public async Task<bool> PingOrchestratorAsync()
    {
        if (!ActivePort.HasValue)
            return false;

        var result = await CheckPortAsync(ActivePort.Value);
        IsOrchestratorOnline = result.IsOnline;
        return result.IsOnline;
    }

    /// <summary>
    /// Aggiunge una porta candidata alla lista
    /// </summary>
    public void AddCandidatePort(int port)
    {
        if (!_candidatePorts.Contains(port))
        {
            _candidatePorts.Add(port);
        }
    }
}
