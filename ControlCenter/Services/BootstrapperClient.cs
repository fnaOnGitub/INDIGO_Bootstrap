using System.Net.Http;
using System.Net.Http.Json;
using ControlCenter.Core.Models;

namespace ControlCenter.Core.Services;

/// <summary>
/// Client HTTP per comunicare con LocalApiServer del Bootstrapper (http://localhost:5000)
/// </summary>
public class BootstrapperClient
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://localhost:5000";

    public BootstrapperClient()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromMinutes(30)
        };
    }

    /// <summary>
    /// GET /api/status - Ottiene stato corrente del cluster
    /// </summary>
    public async Task<ClusterStatus?> GetStatusAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/status");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ClusterStatus>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Errore GetStatus: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// GET /api/logs - Ottiene log recenti
    /// </summary>
    public async Task<List<string>?> GetLogsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/logs");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<LogsResponse>();
            return result?.Logs;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Errore GetLogs: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// GET /api/health - Verifica che il Bootstrapper sia attivo
    /// </summary>
    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// POST /api/command/provision - Esegue provisioning server
    /// </summary>
    public async Task<CommandResponse?> ProvisionAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("/api/command/provision", null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CommandResponse>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Errore Provision: {ex.Message}");
            return new CommandResponse { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// POST /api/command/build-cluster - Genera docker-compose
    /// </summary>
    public async Task<CommandResponse?> BuildClusterAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("/api/command/build-cluster", null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CommandResponse>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Errore BuildCluster: {ex.Message}");
            return new CommandResponse { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// POST /api/command/generate-agents - Genera progetti agenti
    /// </summary>
    public async Task<CommandResponse?> GenerateAgentsAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("/api/command/generate-agents", null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CommandResponse>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Errore GenerateAgents: {ex.Message}");
            return new CommandResponse { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// POST /api/command/configure-communication - Configura RabbitMQ/Redis/JWT
    /// </summary>
    public async Task<CommandResponse?> ConfigureCommunicationAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("/api/command/configure-communication", null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CommandResponse>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Errore ConfigureCommunication: {ex.Message}");
            return new CommandResponse { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// POST /api/command/validate - Valida cluster
    /// </summary>
    public async Task<CommandResponse?> ValidateAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("/api/command/validate", null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CommandResponse>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Errore Validate: {ex.Message}");
            return new CommandResponse { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// POST /api/command/deploy - Workflow completo
    /// </summary>
    public async Task<CommandResponse?> DeployAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("/api/command/deploy", null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CommandResponse>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Errore Deploy: {ex.Message}");
            return new CommandResponse { Success = false, Message = ex.Message };
        }
    }
}

/// <summary>
/// Risposta da comandi API
/// </summary>
public class CommandResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}

/// <summary>
/// Risposta da /api/logs
/// </summary>
public class LogsResponse
{
    public List<string> Logs { get; set; } = new();
}
