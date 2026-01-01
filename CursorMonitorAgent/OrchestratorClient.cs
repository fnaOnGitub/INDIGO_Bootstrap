using System.Net.Http.Json;

namespace CursorMonitorAgent;

/// <summary>
/// Client per comunicare con l'Orchestrator
/// </summary>
public class OrchestratorClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrchestratorClient> _logger;
    private readonly LogBuffer _logBuffer;
    private readonly string _orchestratorUrl;

    public OrchestratorClient(
        HttpClient httpClient,
        ILogger<OrchestratorClient> logger,
        LogBuffer logBuffer)
    {
        _httpClient = httpClient;
        _logger = logger;
        _logBuffer = logBuffer;
        _orchestratorUrl = "http://localhost:5001";
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Invia un task all'Orchestrator
    /// </summary>
    public async Task<DispatchResult> DispatchTaskAsync(string taskName, string payload)
    {
        try
        {
            _logger.LogInformation("Invio task a Orchestrator: {Task}", taskName);
            _logBuffer.Add($"Dispatch task: {taskName}");

            var request = new
            {
                Task = taskName,
                Payload = payload
            };

            var response = await _httpClient.PostAsJsonAsync($"{_orchestratorUrl}/dispatch", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<OrchestratorResponse>();
                
                _logger.LogInformation("Task dispatched con successo: {Task}", taskName);
                _logBuffer.Add($"Task '{taskName}' dispatched con successo");

                return new DispatchResult
                {
                    Success = true,
                    Message = result?.Message ?? "Task dispatched",
                    Worker = result?.Worker ?? "unknown",
                    WorkerType = result?.WorkerType ?? "unknown"
                };
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Errore dispatch task: {StatusCode} - {Error}", response.StatusCode, error);
                _logBuffer.Add($"Errore dispatch: {response.StatusCode}", "ERROR");

                return new DispatchResult
                {
                    Success = false,
                    Message = $"Dispatch failed: {response.StatusCode}",
                    Worker = "",
                    WorkerType = ""
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Eccezione durante dispatch task: {Task}", taskName);
            _logBuffer.Add($"Eccezione dispatch: {ex.Message}", "ERROR");

            return new DispatchResult
            {
                Success = false,
                Message = $"Exception: {ex.Message}",
                Worker = "",
                WorkerType = ""
            };
        }
    }

    /// <summary>
    /// Verifica se l'Orchestrator è raggiungibile
    /// </summary>
    public async Task<bool> IsOrchestratorAliveAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_orchestratorUrl}/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Risposta da Orchestrator
/// </summary>
public class OrchestratorResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? Worker { get; set; }
    public string? WorkerType { get; set; }
    public bool IsAiTask { get; set; }
    public object? WorkerResult { get; set; }
}

/// <summary>
/// Risultato dispatch
/// </summary>
public class DispatchResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string Worker { get; set; } = "";
    public string WorkerType { get; set; } = "";
}
