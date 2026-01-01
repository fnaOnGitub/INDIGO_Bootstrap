using Agent.Worker02;

var builder = WebApplication.CreateBuilder(args);

// Configurazione logging console
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Configura Kestrel per ascoltare solo su HTTP porta 5003
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5003);
});

// Registra WorkerState come singleton
builder.Services.AddSingleton<WorkerState>();

// Registra LogBuffer come singleton
builder.Services.AddSingleton<LogBuffer>();

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
logger.LogInformation("=== Agent.Worker02 avviato ===");
logger.LogInformation("Porta: 5003");
logger.LogInformation("Versione: {Version}", workerState.Version);

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
        Agent = "worker02",
        Uptime = state.Uptime.ToString(@"hh\:mm\:ss"),
        Version = state.Version,
        LastTask = state.LastTask
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

// POST /execute - Esegue task ricevuto
app.MapPost("/execute", async (ExecuteRequest request, WorkerState state, LogBuffer logBuffer, ILogger<Program> log) =>
{
    log.LogInformation("Task ricevuto: Task='{Task}', Payload='{Payload}'", 
        request.Task, 
        request.Payload);
    
    // Log evento
    logBuffer.Add($"Task ricevuto: {request.Task}");
    
    // Aggiorna ultimo task
    state.UpdateLastTask(request.Task);
    
    // Simula esecuzione task
    log.LogInformation("Esecuzione task '{Task}' in corso...", request.Task);
    logBuffer.Add($"Esecuzione task '{request.Task}' in corso...");
    
    await Task.Delay(500);
    
    var result = $"Task '{request.Task}' completato con successo. Payload processato: {request.Payload?.Length ?? 0} caratteri.";
    log.LogInformation("Task '{Task}' completato", request.Task);
    logBuffer.Add($"Task '{request.Task}' completato con successo");
    
    return Results.Ok(new
    {
        Success = true,
        Message = "Task executed",
        Result = result,
        ExecutedTask = request.Task,
        Timestamp = DateTime.UtcNow
    });
})
.WithName("ExecuteTask")
.WithOpenApi();

logger.LogInformation("Agent.Worker02 in ascolto su http://localhost:5003");
logger.LogInformation("Swagger disponibile su http://localhost:5003/swagger");

app.Run();

// ==================== MODELS ====================

/// <summary>
/// Modello per richiesta execute
/// </summary>
public record ExecuteRequest(string Task, string Payload);
