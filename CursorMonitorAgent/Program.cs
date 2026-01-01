using CursorMonitorAgent;

var builder = WebApplication.CreateBuilder(args);

// Configurazione logging console
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Configura Kestrel per ascoltare solo su HTTP porta 5006
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5006);
});

// Registra servizi come singleton
builder.Services.AddSingleton<AgentState>();
builder.Services.AddSingleton<LogBuffer>();
builder.Services.AddSingleton<TaskGenerator>();
builder.Services.AddSingleton<UserDialogService>();
builder.Services.AddSingleton<CursorFileMonitor>();

// Registra HttpClient per OrchestratorClient
builder.Services.AddHttpClient<OrchestratorClient>();

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
var logBuffer = app.Services.GetRequiredService<LogBuffer>();
var fileMonitor = app.Services.GetRequiredService<CursorFileMonitor>();

// Log avvio
logger.LogInformation("=== CursorMonitorAgent avviato ===");
logger.LogInformation("Porta: 5006");
logger.LogInformation("Versione: {Version}", agentState.Version);
logger.LogInformation("Tipo: Autonomous File Monitor");

// Avvia il monitoraggio file system
fileMonitor.Start();

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
        Agent = "cursor-monitor",
        Type = "Autonomous-Monitor",
        Uptime = state.Uptime.ToString(@"hh\:mm\:ss"),
        Version = state.Version,
        LastEvent = state.LastEvent,
        Capabilities = new[]
        {
            "file-system-monitoring",
            "task-generation",
            "user-dialog",
            "multi-cursor-support",
            "autonomous-dispatch"
        }
    });
})
.WithName("GetStatus")
.WithOpenApi();

// GET /logs - Attività recenti
app.MapGet("/logs", (LogBuffer buffer) =>
{
    logger.LogInformation("Log richiesti");
    return Results.Ok(new
    {
        Success = true,
        Count = buffer.GetAll().Count,
        Logs = buffer.GetAll()
    });
})
.WithName("GetLogs")
.WithOpenApi();

// GET /ask-user - Recupera domande pendenti per l'utente
app.MapGet("/ask-user", (UserDialogService dialogService) =>
{
    logger.LogInformation("Recupero domande pendenti per utente");
    var questions = dialogService.GetPendingQuestions();
    
    return Results.Ok(new
    {
        Success = true,
        Count = questions.Count,
        Questions = questions
    });
})
.WithName("GetPendingQuestions")
.WithOpenApi();

// POST /ask-user/answer - Risponde a una domanda
app.MapPost("/ask-user/answer", (AnswerRequest request, UserDialogService dialogService, LogBuffer buffer) =>
{
    logger.LogInformation("Risposta ricevuta per domanda: {Id}", request.QuestionId);
    
    var success = dialogService.AnswerQuestion(request.QuestionId, request.Answer);
    
    if (success)
    {
        buffer.Add($"Risposta ricevuta per domanda {request.QuestionId}");
        return Results.Ok(new
        {
            Success = true,
            Message = "Risposta registrata"
        });
    }
    else
    {
        return Results.Ok(new
        {
            Success = false,
            Message = "Domanda non trovata"
        });
    }
})
.WithName("AnswerQuestion")
.WithOpenApi();

// POST /dispatch-task - Dispatch manuale di un task all'Orchestrator
app.MapPost("/dispatch-task", async (ManualDispatchRequest request, OrchestratorClient orchestratorClient, LogBuffer buffer) =>
{
    logger.LogInformation("Dispatch manuale task: {Task}", request.TaskName);
    buffer.Add($"Dispatch manuale: {request.TaskName}");
    
    var result = await orchestratorClient.DispatchTaskAsync(request.TaskName, request.Payload);
    
    return Results.Ok(new
    {
        Success = result.Success,
        Message = result.Message,
        Worker = result.Worker,
        WorkerType = result.WorkerType
    });
})
.WithName("ManualDispatch")
.WithOpenApi();

// GET /monitored-instances - Lista istanze Cursor monitorate
app.MapGet("/monitored-instances", () =>
{
    logger.LogInformation("Richiesta lista istanze monitorate");
    
    // Recupera lista istanze da FileMonitor (per ora hardcoded)
    var instances = new[]
    {
        new
        {
            Name = "IndigoAiWorker01-CursorBridge",
            Path = "IndigoAiWorker01/bin/Debug/net8.0/CursorBridge",
            IsActive = true,
            Type = "CursorBridge"
        }
    };
    
    return Results.Ok(new
    {
        Success = true,
        Count = instances.Length,
        Instances = instances
    });
})
.WithName("GetMonitoredInstances")
.WithOpenApi();

// POST /ask-user/create - Crea una nuova domanda per l'utente
app.MapPost("/ask-user/create", (CreateQuestionRequest request, UserDialogService dialogService, LogBuffer buffer) =>
{
    logger.LogInformation("Creazione nuova domanda per utente: {Question}", request.Question);
    
    var question = dialogService.AskUser(request.Question, request.Context, request.Options);
    buffer.Add($"Domanda creata: {request.Question}");
    
    return Results.Ok(new
    {
        Success = true,
        Question = question
    });
})
.WithName("CreateQuestion")
.WithOpenApi();

logger.LogInformation("CursorMonitorAgent in ascolto su http://localhost:5006");
logger.LogInformation("Swagger disponibile su http://localhost:5006/swagger");
logger.LogInformation("Monitoraggio file system attivo");

app.Run();

// ==================== MODELS ====================

/// <summary>
/// Richiesta risposta a domanda
/// </summary>
public record AnswerRequest(string QuestionId, string Answer);

/// <summary>
/// Richiesta dispatch manuale
/// </summary>
public record ManualDispatchRequest(string TaskName, string Payload);

/// <summary>
/// Richiesta creazione domanda
/// </summary>
public record CreateQuestionRequest(string Question, string Context, List<string>? Options);
