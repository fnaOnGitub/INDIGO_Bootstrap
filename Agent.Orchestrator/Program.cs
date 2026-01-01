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

// ==================== INTELLIGENT AI ROUTING ====================

/// <summary>
/// Verifica se il task name contiene "ai" (case-insensitive)
/// </summary>
bool TaskNameContainsAi(string taskName)
{
    return taskName.Contains("ai", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Verifica se il payload contiene verbi creativi
/// </summary>
bool IsCreativePayload(string? payload)
{
    if (string.IsNullOrWhiteSpace(payload))
        return false;
    
    var creativeVerbs = new[]
    {
        "crea", "genera", "sviluppa", "costruisci", 
        "implementa", "progetta", "ottimizza", "analizza",
        "create", "generate", "develop", "build",
        "implement", "design", "optimize", "analyze"
    };
    
    var payloadLower = payload.ToLowerInvariant();
    return creativeVerbs.Any(verb => payloadLower.Contains(verb));
}

/// <summary>
/// Verifica se il payload è in linguaggio naturale (non strutturato)
/// </summary>
bool IsNaturalLanguage(string? payload)
{
    if (string.IsNullOrWhiteSpace(payload))
        return false;
    
    var trimmed = payload.Trim();
    
    // Non è linguaggio naturale se inizia con caratteri di struttura dati
    if (trimmed.StartsWith("{") || trimmed.StartsWith("[") || 
        trimmed.StartsWith("<") || trimmed.StartsWith("---"))
    {
        return false;
    }
    
    // È linguaggio naturale se contiene spazi e parole comuni
    var hasSpaces = trimmed.Contains(' ');
    var hasCommonWords = trimmed.Split(' ').Length > 3;
    
    return hasSpaces && hasCommonWords;
}

/// <summary>
/// Metodo avanzato per verificare se un task è di tipo AI
/// </summary>
bool IsAiTask(string task, string? payload = null)
{
    // Lista dei task types espliciti AI
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
    
    // 1. Verifica task types espliciti
    if (aiTaskTypes.Contains(task.ToLowerInvariant()))
    {
        logger.LogInformation("✓ AI Task rilevato: Task type esplicito '{Task}'", task);
        return true;
    }
    
    // 2. Verifica se task name contiene "ai"
    if (TaskNameContainsAi(task))
    {
        logger.LogInformation("✓ AI Task rilevato: Task name contiene 'ai' ('{Task}')", task);
        return true;
    }
    
    // 3. Verifica payload con verbi creativi
    if (IsCreativePayload(payload))
    {
        logger.LogInformation("✓ AI Task rilevato: Payload contiene verbi creativi");
        return true;
    }
    
    // 4. Verifica se è linguaggio naturale
    if (IsNaturalLanguage(payload))
    {
        logger.LogInformation("✓ AI Task rilevato: Payload è linguaggio naturale");
        return true;
    }
    
    logger.LogInformation("✗ Task classificato come standard: '{Task}'", task);
    return false;
}

/// <summary>
/// Determina il worker appropriato con routing intelligente
/// </summary>
string GetWorkerForTask(string task, string? payload = null)
{
    logger.LogInformation("=== AI ROUTING ATTIVATO ===");
    logger.LogInformation("Analisi task: '{Task}'", task);
    
    if (!string.IsNullOrWhiteSpace(payload))
    {
        var previewLength = Math.Min(payload.Length, 100);
        var preview = payload.Substring(0, previewLength);
        if (payload.Length > 100) preview += "...";
        logger.LogInformation("Payload preview: {Preview}", preview);
    }
    
    if (IsAiTask(task, payload))
    {
        logger.LogInformation(">>> Task classificato come AI");
        logger.LogInformation(">>> Instradato a Worker AI (IndigoAiWorker01)");
        return aiWorkers[0];
    }
    else
    {
        var workerUrl = GetNextWorkerUrl();
        logger.LogInformation(">>> Task classificato come Standard");
        logger.LogInformation(">>> Instradato a Worker Standard (round-robin): {Worker}", workerUrl);
        return workerUrl;
    }
}

logger.LogInformation("=== INTELLIGENT AI ROUTING CONFIGURATO ===");
logger.LogInformation("Criteri di classificazione AI:");
logger.LogInformation("  ✓ Task type esplicito (generate-code, optimize-prompt, etc.)");
logger.LogInformation("  ✓ Task name contiene 'ai' (case-insensitive)");
logger.LogInformation("  ✓ Payload con verbi creativi (crea, genera, sviluppa, ottimizza, etc.)");
logger.LogInformation("  ✓ Payload in linguaggio naturale (non JSON/YAML/XML)");
logger.LogInformation("");
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
    
    // Determina il worker appropriato (AI o standard) con analisi intelligente
    var workerUrl = GetWorkerForTask(request.Task, request.Payload);
    var isAiTask = IsAiTask(request.Task, request.Payload);
    var workerType = isAiTask ? "AI-Worker" : "Standard-Worker";
    log.LogInformation("Task inoltrato a {WorkerType}: {WorkerUrl}", workerType, workerUrl);
    
    // Log routing dettagliato
    if (isAiTask)
    {
        logBuffer.Add($"AI Routing attivato → Task classificato come AI");
        logBuffer.Add($"Instradato a Worker AI: {workerUrl}");
    }
    else
    {
        logBuffer.Add($"Instradato a {workerType}: {workerUrl}");
    }
    
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
                IsAiTask = isAiTask,
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
