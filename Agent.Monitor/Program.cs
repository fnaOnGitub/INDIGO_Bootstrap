using Agent.Monitor;

var builder = WebApplication.CreateBuilder(args);

// Configurazione logging console
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Configura Kestrel per ascoltare solo su HTTP porta 5004
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5004);
});

// Registra MonitorState come singleton
builder.Services.AddSingleton<MonitorState>();

// Registra HttpClient per chiamare gli altri agenti
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
var monitorState = app.Services.GetRequiredService<MonitorState>();
var httpClientFactory = app.Services.GetRequiredService<IHttpClientFactory>();

// Log avvio
logger.LogInformation("=== Agent.Monitor avviato ===");
logger.LogInformation("Porta: 5004");
logger.LogInformation("Versione: {Version}", monitorState.Version);

// ==================== CLUSTER CONFIGURATION ====================

// Lista degli agenti da monitorare
var clusterAgents = new[]
{
    new { Name = "orchestrator", Url = "http://localhost:5001" },
    new { Name = "worker01", Url = "http://localhost:5002" },
    new { Name = "worker02", Url = "http://localhost:5003" },
    new { Name = "indigoaiworker01", Url = "http://localhost:5005" }
};

logger.LogInformation("Monitoraggio cluster configurato per {Count} agenti", clusterAgents.Length);
foreach (var agent in clusterAgents)
{
    logger.LogInformation("  - {Name}: {Url}", agent.Name, agent.Url);
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
app.MapGet("/status", (MonitorState state) =>
{
    logger.LogInformation("Status richiesto");
    return Results.Ok(new
    {
        Agent = "monitor",
        Uptime = state.Uptime.ToString(@"hh\:mm\:ss"),
        Version = state.Version
    });
})
.WithName("GetStatus")
.WithOpenApi();

// GET /cluster/health - Aggrega health di tutti gli agenti
app.MapGet("/cluster/health", async (ILogger<Program> log) =>
{
    log.LogInformation("Cluster health check richiesto");
    
    var httpClient = httpClientFactory.CreateClient();
    httpClient.Timeout = TimeSpan.FromSeconds(5);
    
    var agents = new List<AgentInfo>();
    
    foreach (var agent in clusterAgents)
    {
        try
        {
            log.LogInformation("Controllo health di {Name} ({Url})", agent.Name, agent.Url);
            var response = await httpClient.GetAsync($"{agent.Url}/health");
            
            if (response.IsSuccessStatusCode)
            {
                var healthData = await response.Content.ReadFromJsonAsync<object>();
                agents.Add(new AgentInfo
                {
                    Name = agent.Name,
                    Url = agent.Url,
                    Health = healthData
                });
                log.LogInformation("✓ {Name} è online", agent.Name);
            }
            else
            {
                agents.Add(new AgentInfo
                {
                    Name = agent.Name,
                    Url = agent.Url,
                    Health = new { Status = "ERROR", Message = $"HTTP {response.StatusCode}" }
                });
                log.LogWarning("✗ {Name} ha restituito {StatusCode}", agent.Name, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            agents.Add(new AgentInfo
            {
                Name = agent.Name,
                Url = agent.Url,
                Health = new { Status = "OFFLINE", Message = ex.Message }
            });
            log.LogError("✗ {Name} non raggiungibile: {Error}", agent.Name, ex.Message);
        }
    }
    
    return Results.Ok(new
    {
        Success = true,
        Timestamp = DateTime.UtcNow,
        Agents = agents
    });
})
.WithName("ClusterHealth")
.WithOpenApi();

// GET /cluster/status - Aggrega status di tutti gli agenti
app.MapGet("/cluster/status", async (ILogger<Program> log) =>
{
    log.LogInformation("Cluster status richiesto");
    
    var httpClient = httpClientFactory.CreateClient();
    httpClient.Timeout = TimeSpan.FromSeconds(5);
    
    var agents = new List<AgentStatusInfo>();
    
    foreach (var agent in clusterAgents)
    {
        try
        {
            log.LogInformation("Controllo status di {Name} ({Url})", agent.Name, agent.Url);
            var response = await httpClient.GetAsync($"{agent.Url}/status");
            
            if (response.IsSuccessStatusCode)
            {
                var statusData = await response.Content.ReadFromJsonAsync<object>();
                agents.Add(new AgentStatusInfo
                {
                    Name = agent.Name,
                    Url = agent.Url,
                    Status = statusData
                });
                log.LogInformation("✓ {Name} status ottenuto", agent.Name);
            }
            else
            {
                agents.Add(new AgentStatusInfo
                {
                    Name = agent.Name,
                    Url = agent.Url,
                    Status = new { Status = "ERROR", Message = $"HTTP {response.StatusCode}" }
                });
                log.LogWarning("✗ {Name} ha restituito {StatusCode}", agent.Name, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            agents.Add(new AgentStatusInfo
            {
                Name = agent.Name,
                Url = agent.Url,
                Status = new { Status = "OFFLINE", Message = ex.Message }
            });
            log.LogError("✗ {Name} non raggiungibile: {Error}", agent.Name, ex.Message);
        }
    }
    
    return Results.Ok(new
    {
        Success = true,
        Timestamp = DateTime.UtcNow,
        Agents = agents
    });
})
.WithName("ClusterStatus")
.WithOpenApi();

logger.LogInformation("Agent.Monitor in ascolto su http://localhost:5004");
logger.LogInformation("Swagger disponibile su http://localhost:5004/swagger");

app.Run();

// ==================== MODELS ====================

/// <summary>
/// Informazioni su un agente (health)
/// </summary>
public class AgentInfo
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public object? Health { get; set; }
}

/// <summary>
/// Informazioni su un agente (status)
/// </summary>
public class AgentStatusInfo
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public object? Status { get; set; }
}
