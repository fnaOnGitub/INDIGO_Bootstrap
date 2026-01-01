using Agent.Orchestrator;
using System.Net.Http.Json;

var builder = WebApplication.CreateBuilder(args);

// Configurazione logging console
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Configura Kestrel per ascoltare solo su HTTP porta 5001
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5001);
});

// Registra AgentState come singleton
builder.Services.AddSingleton<AgentState>();

// Registra LogBuffer come singleton
builder.Services.AddSingleton<LogBuffer>();

// Registra HttpClient per chiamare i workers
builder.Services.AddHttpClient();

// Configurazione JSON Serialization - Usa PascalCase
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null; // null = PascalCase
});

// Swagger per documentazione API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Abilita Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var agentState = app.Services.GetRequiredService<AgentState>();
var httpClientFactory = app.Services.GetRequiredService<IHttpClientFactory>();

// Log avvio
logger.LogInformation("=== Agent.Orchestrator avviato ===");
logger.LogInformation("Porta: 5001");
logger.LogInformation("Versione: {Version}", agentState.Version);

// ==================== LOAD BALANCING CONFIGURATION ====================

// Lista dei workers standard (task generici)
var workers = new[] { "http://localhost:5002", "http://localhost:5003" };
var workerIndex = 0;
var workerLock = new object();

// Lista dei workers AI (task AI)
var aiWorkers = new[] { "http://localhost:5005" };

// Metodo per ottenere il prossimo worker standard (round-robin)
string GetNextWorkerUrl()
{
    lock (workerLock)
    {
        var url = workers[workerIndex];
        workerIndex = (workerIndex + 1) % workers.Length;
        return url;
    }
}

// Metodo per verificare se un task è di tipo AI
bool IsAiTask(string task)
{
    var aiTaskTypes = new[]
    {
        "generate-code",
        "refactor-code",
        "explain-code",
        "create-component",
        "fix-snippet",
        "cursor-prompt",
        "optimize-prompt"
    };
    
    return aiTaskTypes.Contains(task.ToLowerInvariant());
}

// Metodo per ottenere il worker appropriato per un task
string GetWorkerForTask(string task)
{
    if (IsAiTask(task))
    {
        logger.LogInformation("Task AI riconosciuto: {Task} → IndigoAiWorker01", task);
        return aiWorkers[0];
    }
    else
    {
        var workerUrl = GetNextWorkerUrl();
        logger.LogInformation("Task standard: {Task} → Worker operativo (round-robin)", task);
        return workerUrl;
    }
}

logger.LogInformation("Load balancing configurato:");
logger.LogInformation("  - Workers standard: {Count}", workers.Length);
foreach (var worker in workers)
{
    logger.LogInformation("    • {Worker}", worker);
}
logger.LogInformation("  - Workers AI: {Count}", aiWorkers.Length);
foreach (var aiWorker in aiWorkers)
{
    logger.LogInformation("    • {AiWorker} (AI-Powered)", aiWorker);
}

// ==================== ENDPOINTS ====================

// GET /health - Health check
app.MapGet("/health", () =>
{
    logger.LogInformation("Health check richiesto");
    return Results.Ok(new
    {
        Status = "OK",
        Timestamp = DateTime.UtcNow
    });
})
.WithName("HealthCheck")
.WithOpenApi();

// GET /status - Stato dettagliato agente
app.MapGet("/status", (AgentState state) =>
{
    logger.LogInformation("Status richiesto");
    return Results.Ok(new
    {
        Agent = "orchestrator",
        Uptime = state.Uptime.ToString(@"hh\:mm\:ss"),
        Version = state.Version,
        LastCommand = state.LastCommand
    });
})
.WithName("GetStatus")
.WithOpenApi();

// GET /logs - Attività recenti
app.MapGet("/logs", (LogBuffer logBuffer) =>
{
    logger.LogInformation("Log richiesti");
    return Results.Ok(new
    {
        Success = true,
        Count = logBuffer.GetAll().Count,
        Logs = logBuffer.GetAll()
    });
})
.WithName("GetLogs")
.WithOpenApi();

// POST /dispatch - Riceve task e lo inoltra a un worker (routing intelligente)
app.MapPost("/dispatch", async (DispatchRequest request, AgentState state, LogBuffer logBuffer, ILogger<Program> log) =>
{
    log.LogInformation("Dispatch ricevuto: Task='{Task}', Payload length={Length}", 
        request.Task, 
        request.Payload?.Length ?? 0);
    
    // Log evento
    logBuffer.Add($"Task ricevuto: {request.Task}");
    
    // Aggiorna ultimo comando
    state.UpdateLastCommand(request.Task);
    
    // Determina il worker appropriato (AI o standard)
    var workerUrl = GetWorkerForTask(request.Task);
    var workerType = IsAiTask(request.Task) ? "AI-Worker" : "Standard-Worker";
    log.LogInformation("Task inoltrato a {WorkerType}: {WorkerUrl}", workerType, workerUrl);
    
    // Log routing
    logBuffer.Add($"Instradato a {workerType}: {workerUrl}");
    
    try
    {
        // Crea HttpClient
        var httpClient = httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30);
        
        // Prepara richiesta per il worker
        var workerRequest = new
        {
            Task = request.Task,
            Payload = request.Payload
        };
        
        // Chiama POST {workerUrl}/execute
        var response = await httpClient.PostAsJsonAsync($"{workerUrl}/execute", workerRequest);
        
        if (response.IsSuccessStatusCode)
        {
            // Leggi risposta dal worker
            var workerResult = await response.Content.ReadFromJsonAsync<WorkerResponse>();
            
            log.LogInformation("{WorkerType} {WorkerUrl} ha completato il task con successo", workerType, workerUrl);
            
            // Log completamento
            logBuffer.Add($"Task '{request.Task}' completato con successo da {workerType}");
            
            return Results.Ok(new
            {
                Success = true,
                Message = $"Task dispatched to {workerType}",
                Worker = workerUrl,
                WorkerType = workerType,
                IsAiTask = IsAiTask(request.Task),
                WorkerResult = workerResult
            });
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            log.LogError("Worker {WorkerUrl} ha restituito errore: {StatusCode} - {Error}", 
                workerUrl, response.StatusCode, errorContent);
            
            // Log errore
            logBuffer.Add($"Errore da worker: {response.StatusCode}", "ERROR");
            
            return Results.Ok(new
            {
                Success = false,
                Message = $"Worker error: {response.StatusCode}",
                Worker = workerUrl,
                WorkerResult = (object?)null
            });
        }
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Errore durante dispatch a worker {WorkerUrl}", workerUrl);
        
        // Log errore
        logBuffer.Add($"Errore dispatch: {ex.Message}", "ERROR");
        
        return Results.Ok(new
        {
            Success = false,
            Message = $"Error dispatching to worker: {ex.Message}",
            Worker = workerUrl,
            WorkerResult = (object?)null
        });
    }
})
.WithName("DispatchTask")
.WithOpenApi();

logger.LogInformation("Agent.Orchestrator in ascolto su http://localhost:5001");
logger.LogInformation("Swagger disponibile su http://localhost:5001/swagger");

app.Run();

// ==================== MODELS ====================

/// <summary>
/// Modello per richiesta dispatch
/// </summary>
public record DispatchRequest(string Task, string Payload);

/// <summary>
/// Modello per risposta dal worker
/// </summary>
public class WorkerResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string Result { get; set; } = "";
    public string ExecutedTask { get; set; } = "";
    public DateTime Timestamp { get; set; }
}
