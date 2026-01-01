using IndigoAiWorker01;

var builder = WebApplication.CreateBuilder(args);

// Configurazione logging console
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Configura Kestrel per ascoltare solo su HTTP porta 5005
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5005);
});

// Registra servizi come singleton
builder.Services.AddSingleton<WorkerState>();
builder.Services.AddSingleton<LogBuffer>();
builder.Services.AddSingleton<PromptOptimizer>();
builder.Services.AddSingleton<AiEngine>();
builder.Services.AddSingleton<CursorBridge>();

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
var workerState = app.Services.GetRequiredService<WorkerState>();

// Log avvio
logger.LogInformation("=== IndigoAiWorker01 avviato ===");
logger.LogInformation("Porta: 5005");
logger.LogInformation("Versione: {Version}", workerState.Version);
logger.LogInformation("Tipo: AI-Powered Worker");

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
app.MapGet("/status", (WorkerState state) =>
{
    logger.LogInformation("Status richiesto");
    return Results.Ok(new
    {
        Agent = "IndigoAiWorker01",
        Type = "AI-Worker",
        Uptime = state.Uptime.ToString(@"hh\:mm\:ss"),
        Version = state.Version,
        LastTask = state.LastTask,
        Capabilities = new[]
        {
            "generate-code",
            "refactor-code",
            "explain-code",
            "create-component",
            "fix-snippet",
            "cursor-prompt",
            "optimize-prompt"
        }
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

// POST /execute - Esegue task ricevuto con dispatcher AI
app.MapPost("/execute", async (ExecuteRequest request, WorkerState state, LogBuffer logBuffer, AiEngine aiEngine, CursorBridge cursorBridge, ILogger<Program> log) =>
{
    log.LogInformation("Task AI ricevuto: Task='{Task}', Payload length={Length}", 
        request.Task, 
        request.Payload?.Length ?? 0);
    
    // Log evento
    logBuffer.Add($"Task AI ricevuto: {request.Task}");
    
    // Aggiorna ultimo task
    state.UpdateLastTask(request.Task);
    
    // Variabili per risposta
    string result;
    string? optimizedPrompt = null;
    bool cursorFileWritten = false;
    string cursorFilePath = "";
    
    // Dispatcher AI Task
    switch (request.Task.ToLowerInvariant())
    {
        case "generate-code":
            log.LogInformation("Esecuzione: GenerateCode");
            logBuffer.Add("Generazione codice in corso...");
            result = aiEngine.GenerateCode(request.Payload ?? "");
            
            // FILE ALWAYS MODE: Scrivi SEMPRE file output
            try
            {
                cursorFilePath = cursorBridge.WriteAiOutput(
                    taskName: "generate-code",
                    payload: request.Payload ?? "",
                    aiResult: result
                );
                cursorFileWritten = true;
                logBuffer.Add($"File generato: {System.IO.Path.GetFileName(cursorFilePath)}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Errore scrittura file output");
                logBuffer.Add($"Errore scrittura file: {ex.Message}", "ERROR");
            }
            break;

        case "refactor-code":
            log.LogInformation("Esecuzione: RefactorCode");
            logBuffer.Add("Refactoring codice in corso...");
            result = aiEngine.RefactorCode(request.Payload ?? "");
            
            // FILE ALWAYS MODE: Scrivi SEMPRE file output
            try
            {
                cursorFilePath = cursorBridge.WriteAiOutput(
                    taskName: "refactor-code",
                    payload: request.Payload ?? "",
                    aiResult: result
                );
                cursorFileWritten = true;
                logBuffer.Add($"File generato: {System.IO.Path.GetFileName(cursorFilePath)}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Errore scrittura file output");
                logBuffer.Add($"Errore scrittura file: {ex.Message}", "ERROR");
            }
            break;

        case "explain-code":
            log.LogInformation("Esecuzione: ExplainCode");
            logBuffer.Add("Analisi e spiegazione codice in corso...");
            result = aiEngine.ExplainCode(request.Payload ?? "");
            
            // FILE ALWAYS MODE: Scrivi SEMPRE file output
            try
            {
                cursorFilePath = cursorBridge.WriteAiOutput(
                    taskName: "explain-code",
                    payload: request.Payload ?? "",
                    aiResult: result
                );
                cursorFileWritten = true;
                logBuffer.Add($"File generato: {System.IO.Path.GetFileName(cursorFilePath)}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Errore scrittura file output");
                logBuffer.Add($"Errore scrittura file: {ex.Message}", "ERROR");
            }
            break;

        case "create-component":
            log.LogInformation("Esecuzione: CreateComponent");
            logBuffer.Add("Creazione componente in corso...");
            result = aiEngine.CreateComponent(request.Payload ?? "");
            
            // FILE ALWAYS MODE: Scrivi SEMPRE file output
            try
            {
                cursorFilePath = cursorBridge.WriteAiOutput(
                    taskName: "create-component",
                    payload: request.Payload ?? "",
                    aiResult: result
                );
                cursorFileWritten = true;
                logBuffer.Add($"File generato: {System.IO.Path.GetFileName(cursorFilePath)}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Errore scrittura file output");
                logBuffer.Add($"Errore scrittura file: {ex.Message}", "ERROR");
            }
            break;

        case "fix-snippet":
            log.LogInformation("Esecuzione: FixSnippet");
            logBuffer.Add("Correzione snippet in corso...");
            result = aiEngine.FixSnippet(request.Payload ?? "");
            
            // FILE ALWAYS MODE: Scrivi SEMPRE file output
            try
            {
                cursorFilePath = cursorBridge.WriteAiOutput(
                    taskName: "fix-snippet",
                    payload: request.Payload ?? "",
                    aiResult: result
                );
                cursorFileWritten = true;
                logBuffer.Add($"File generato: {System.IO.Path.GetFileName(cursorFilePath)}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Errore scrittura file output");
                logBuffer.Add($"Errore scrittura file: {ex.Message}", "ERROR");
            }
            break;

        case "cursor-prompt":
            log.LogInformation("Esecuzione: CursorPrompt");
            logBuffer.Add("Generazione prompt Cursor in corso...");
            result = aiEngine.GenerateCursorPrompt("custom-request", request.Payload ?? "");
            
            // FILE ALWAYS MODE: Scrivi SEMPRE file output
            try
            {
                cursorFilePath = cursorBridge.WriteAiOutput(
                    taskName: "cursor-prompt",
                    payload: request.Payload ?? "",
                    aiResult: result
                );
                cursorFileWritten = true;
                logBuffer.Add($"File generato: {System.IO.Path.GetFileName(cursorFilePath)}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Errore scrittura file output");
                logBuffer.Add($"Errore scrittura file: {ex.Message}", "ERROR");
            }
            break;

        case "optimize-prompt":
            log.LogInformation("Esecuzione: OptimizePrompt");
            logBuffer.Add($"Ottimizzazione prompt in corso...");
            
            // Ottimizza l'intento dell'utente
            optimizedPrompt = aiEngine.OptimizePrompt(request.Payload ?? "");
            result = optimizedPrompt;
            
            // FILE ALWAYS MODE: Scrivi SEMPRE file output (con prompt ottimizzato)
            try
            {
                cursorFilePath = cursorBridge.WriteAiOutput(
                    taskName: "optimize-prompt",
                    payload: request.Payload ?? "",
                    aiResult: result,
                    optimizedPrompt: optimizedPrompt
                );
                cursorFileWritten = true;
                log.LogInformation("Prompt ottimizzato generato e salvato: {FilePath}", cursorFilePath);
                logBuffer.Add($"Prompt ottimizzato generato e salvato: {System.IO.Path.GetFileName(cursorFilePath)}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Errore scrittura prompt ottimizzato");
                logBuffer.Add($"Errore scrittura prompt: {ex.Message}", "ERROR");
                cursorFileWritten = false;
                cursorFilePath = "";
            }
            break;

        default:
            log.LogWarning("Task non riconosciuto: {Task}", request.Task);
            logBuffer.Add($"Task non riconosciuto: {request.Task}", "WARN");
            result = $"Task '{request.Task}' non supportato. Task disponibili: generate-code, refactor-code, explain-code, create-component, fix-snippet, cursor-prompt, optimize-prompt";
            break;
    }
    
    // Simula delay esecuzione AI
    await Task.Delay(300);
    
    log.LogInformation("Task '{Task}' completato", request.Task);
    logBuffer.Add($"Task AI '{request.Task}' completato con successo");
    
    // Costruisci risposta completa (FILE ALWAYS MODE)
    var response = new Dictionary<string, object?>
    {
        { "Success", true },
        { "Message", "AI Task executed (FILE ALWAYS MODE)" },
        { "Result", result },
        { "ExecutedTask", request.Task },
        { "Timestamp", DateTime.UtcNow },
        { "CursorFileWritten", cursorFileWritten },
        { "CursorFilePath", cursorFilePath }
    };
    
    // Aggiungi OptimizedPrompt se presente
    if (!string.IsNullOrEmpty(optimizedPrompt))
    {
        response.Add("OptimizedPrompt", optimizedPrompt);
    }
    
    // Aggiungi AiOutputText (sempre presente)
    response.Add("AiOutputText", result);
    
    return Results.Ok(response);
})
.WithName("ExecuteAiTask")
.WithOpenApi();

// GET /cursor/bridge-files - Lista file nella CursorBridge
app.MapGet("/cursor/bridge-files", (CursorBridge bridge) =>
{
    logger.LogInformation("Lista file CursorBridge richiesta");
    var files = bridge.ListBridgeFiles();
    
    return Results.Ok(new
    {
        Success = true,
        Count = files.Count,
        Files = files
    });
})
.WithName("ListBridgeFiles")
.WithOpenApi();

// POST /cursor/cleanup - Pulisce file vecchi
app.MapPost("/cursor/cleanup", (CursorBridge bridge, ILogger<Program> log) =>
{
    log.LogInformation("Cleanup CursorBridge richiesto");
    var deleted = bridge.CleanupOldFiles(7);
    
    return Results.Ok(new
    {
        Success = true,
        Message = $"Cleanup completato: {deleted} file eliminati",
        DeletedCount = deleted
    });
})
.WithName("CleanupBridge")
.WithOpenApi();

logger.LogInformation("IndigoAiWorker01 in ascolto su http://localhost:5005");
logger.LogInformation("Swagger disponibile su http://localhost:5005/swagger");
logger.LogInformation("CursorBridge pronto per integrazione con Cursor");

app.Run();

// ==================== MODELS ====================

/// <summary>
/// Modello per richiesta execute
/// </summary>
public record ExecuteRequest(string Task, string Payload);
