// Template per agente IndigoLab - .NET 8 Minimal API
// Questo file viene utilizzato da AgentGenerator per creare nuovi agenti

using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configurazione Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/agent-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Configurazione servizi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// === ENDPOINTS ===

// Health check endpoint
app.MapGet("/health", () =>
{
    Log.Debug("Health check richiesto");
    return Results.Ok(new
    {
        status = "healthy",
        agent = Environment.GetEnvironmentVariable("AGENT_NAME") ?? "unknown",
        role = Environment.GetEnvironmentVariable("AGENT_ROLE") ?? "unknown",
        timestamp = DateTime.UtcNow
    });
});

// Metadata endpoint
app.MapGet("/metadata", () =>
{
    Log.Debug("Metadata richiesto");
    return Results.Ok(new
    {
        agentName = Environment.GetEnvironmentVariable("AGENT_NAME") ?? "unknown",
        agentRole = Environment.GetEnvironmentVariable("AGENT_ROLE") ?? "unknown",
        version = "1.0.0",
        dotnetVersion = Environment.Version.ToString(),
        startTime = DateTime.UtcNow
    });
});

// Echo endpoint (per test comunicazione)
app.MapPost("/echo", async (HttpContext context) =>
{
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    Log.Information($"Echo ricevuto: {body}");
    return Results.Ok(new { echo = body, timestamp = DateTime.UtcNow });
});

// Log endpoint (riceve log da altri agenti)
app.MapPost("/log", async (HttpContext context) =>
{
    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
    Log.Information($"Log ricevuto: {body}");
    return Results.Ok(new { received = true });
});

Log.Information($"Agente {{AgentName}} avviato", 
    Environment.GetEnvironmentVariable("AGENT_NAME") ?? "unknown");

app.Run();
