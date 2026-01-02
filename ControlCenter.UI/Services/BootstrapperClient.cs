using System.Net.Http;
using System.Net.Http.Json;
using ControlCenter.UI.Models;

namespace ControlCenter.UI.Services;

/// <summary>
/// Client HTTP per comunicare con l'Orchestrator (porta dinamica)
/// </summary>
public class BootstrapperClient
{
    private readonly HttpClient _httpClient;
    private string _orchestratorBaseUrl = "http://localhost:5001";

    public string OrchestratorUrl => _orchestratorBaseUrl;

    public BootstrapperClient()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    /// <summary>
    /// Aggiorna l'URL dell'Orchestrator
    /// </summary>
    public void UpdateOrchestratorUrl(string url)
    {
        _orchestratorBaseUrl = url;
    }

    /// <summary>
    /// Aggiorna la porta dell'Orchestrator
    /// </summary>
    public void UpdateOrchestratorPort(int port)
    {
        _orchestratorBaseUrl = $"http://localhost:{port}";
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

    /// <summary>
    /// POST /api/agents/{agentName}/test - Testa un agente specifico
    /// </summary>
    public async Task<TestAgentResponse> TestAgentAsync(string agentName)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/agents/{agentName}/test", null);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TestAgentResponse>();
                return result ?? new TestAgentResponse { Success = true, Message = "Test completato con successo" };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new TestAgentResponse 
                { 
                    Success = false, 
                    Message = $"Test fallito: {response.StatusCode} - {errorContent}" 
                };
            }
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Errore TestAgent: {ex.Message}");
            return new TestAgentResponse 
            { 
                Success = false, 
                Message = $"Errore di connessione: {ex.Message}. Assicurati che il Bootstrapper sia attivo su http://localhost:5000" 
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Errore TestAgent: {ex.Message}");
            return new TestAgentResponse 
            { 
                Success = false, 
                Message = $"Errore: {ex.Message}" 
            };
        }
    }

    /// <summary>
    /// POST http://localhost:5001/dispatch - Invia task all'Orchestrator
    /// </summary>
    public async Task<DispatchResponse> DispatchTaskAsync(string agentName, string task, string payload, string? targetPath = null)
    {
        try
        {
            // Se c'è un targetPath, crea un payload strutturato, altrimenti usa il payload come stringa
            object payloadObject;
            
            if (!string.IsNullOrEmpty(targetPath))
            {
                payloadObject = new
                {
                    UserRequest = payload,
                    TargetPath = targetPath
                };
            }
            else
            {
                payloadObject = payload;
            }

            var requestBody = new
            {
                Task = task,
                Payload = payloadObject
            };

            var content = JsonContent.Create(requestBody);
            var response = await _httpClient.PostAsync($"{_orchestratorBaseUrl}/dispatch", content);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<DispatchResponse>();
                return result ?? new DispatchResponse 
                { 
                    Success = true, 
                    Message = "Task dispatched con successo" 
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new DispatchResponse 
                { 
                    Success = false, 
                    Message = $"Dispatch fallito: {response.StatusCode} - {errorContent}" 
                };
            }
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Errore DispatchTask: {ex.Message}");
            return new DispatchResponse 
            { 
                Success = false, 
                Message = $"Errore di connessione all'Orchestrator: {ex.Message}. Assicurati che l'Orchestrator sia attivo su http://localhost:5001" 
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Errore DispatchTask: {ex.Message}");
            return new DispatchResponse 
            { 
                Success = false, 
                Message = $"Errore: {ex.Message}" 
            };
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

/// <summary>
/// Risposta da /api/agents/{name}/test
/// </summary>
public class TestAgentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? Details { get; set; }
}

/// <summary>
/// Risposta da POST http://localhost:5001/dispatch
/// </summary>
public class DispatchResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? Worker { get; set; }
    public string? WorkerType { get; set; }
    public bool IsAiTask { get; set; }
    public System.Text.Json.JsonElement? WorkerResult { get; set; }
    public bool RequiresUserConfirmation { get; set; }
    public ProposalData? ProposalData { get; set; }
}

/// <summary>
/// Dati del proposal per conferma utente
/// </summary>
public class ProposalData
{
    public List<string> Features { get; set; } = new();
    public string ProposedStructure { get; set; } = "";
    public List<string> Modules { get; set; } = new();
    public string ProposalText { get; set; } = "";
}
