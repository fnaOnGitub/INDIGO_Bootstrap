using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Text.Json;

namespace Bootstrapper.Modules;

/// <summary>
/// Server API HTTP locale per comunicazione con WinUI 3 Control Center
/// Espone endpoint per: stato cluster, comandi, log di alto livello
/// </summary>
public class LocalApiServer
{
    private readonly ProvisioningEngine _provisioningEngine;
    private readonly ClusterBuilder _clusterBuilder;
    private readonly AgentGenerator _agentGenerator;
    private readonly CommunicationConfigurator _communicationConfigurator;
    private readonly ValidationSuite _validationSuite;

    private ClusterStatus _clusterStatus = new();
    private readonly List<string> _recentLogs = new();
    private const int MaxRecentLogs = 100;

    public LocalApiServer(
        ProvisioningEngine provisioningEngine,
        ClusterBuilder clusterBuilder,
        AgentGenerator agentGenerator,
        CommunicationConfigurator communicationConfigurator,
        ValidationSuite validationSuite)
    {
        _provisioningEngine = provisioningEngine;
        _clusterBuilder = clusterBuilder;
        _agentGenerator = agentGenerator;
        _communicationConfigurator = communicationConfigurator;
        _validationSuite = validationSuite;
    }

    /// <summary>
    /// Avvia il server HTTP locale su porta 5000
    /// </summary>
    public void Start()
    {
        var builder = WebApplication.CreateBuilder();
        
        // Configura Kestrel per ascoltare su localhost:5000
        builder.WebHost.UseUrls("http://localhost:5000");
        
        var app = builder.Build();

        // Endpoint: GET /api/status
        app.MapGet("/api/status", () =>
        {
            Log.Debug("API: GET /api/status");
            return Results.Ok(_clusterStatus);
        });

        // Endpoint: GET /api/logs
        app.MapGet("/api/logs", () =>
        {
            Log.Debug("API: GET /api/logs");
            return Results.Ok(new { logs = _recentLogs });
        });

        // Endpoint: POST /api/command/provision
        app.MapPost("/api/command/provision", async (HttpContext context) =>
        {
            Log.Information("API: POST /api/command/provision");
            
            try
            {
                var configLoader = new ConfigurationLoader();
                var serversConfig = configLoader.LoadServersConfig("Config/servers.yaml");

                _clusterStatus.IsProvisioning = true;
                _clusterStatus.LastCommand = "provision";
                _clusterStatus.LastCommandTime = DateTime.UtcNow;

                await _provisioningEngine.ProvisionServers(serversConfig);

                _clusterStatus.IsProvisioning = false;
                _clusterStatus.ProvisioningCompleted = true;
                AddLog("Provisioning completato con successo");

                return Results.Ok(new { success = true, message = "Provisioning completato" });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Errore durante provisioning via API");
                _clusterStatus.IsProvisioning = false;
                AddLog($"ERRORE provisioning: {ex.Message}");
                return Results.Problem(ex.Message);
            }
        });

        // Endpoint: POST /api/command/build-cluster
        app.MapPost("/api/command/build-cluster", async (HttpContext context) =>
        {
            Log.Information("API: POST /api/command/build-cluster");
            
            try
            {
                var configLoader = new ConfigurationLoader();
                var clusterConfig = configLoader.LoadClusterConfig("Config/cluster.yaml");

                _clusterStatus.IsBuildingCluster = true;
                _clusterStatus.LastCommand = "build-cluster";
                _clusterStatus.LastCommandTime = DateTime.UtcNow;

                await _clusterBuilder.BuildCluster(clusterConfig);

                _clusterStatus.IsBuildingCluster = false;
                _clusterStatus.ClusterBuilt = true;
                AddLog("Cluster build completato con successo");

                return Results.Ok(new { success = true, message = "Cluster build completato" });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Errore durante build cluster via API");
                _clusterStatus.IsBuildingCluster = false;
                AddLog($"ERRORE build cluster: {ex.Message}");
                return Results.Problem(ex.Message);
            }
        });

        // Endpoint: POST /api/command/generate-agents
        app.MapPost("/api/command/generate-agents", async (HttpContext context) =>
        {
            Log.Information("API: POST /api/command/generate-agents");
            
            try
            {
                var configLoader = new ConfigurationLoader();
                var clusterConfig = configLoader.LoadClusterConfig("Config/cluster.yaml");

                _clusterStatus.IsGeneratingAgents = true;
                _clusterStatus.LastCommand = "generate-agents";
                _clusterStatus.LastCommandTime = DateTime.UtcNow;

                await _agentGenerator.GenerateAgents(clusterConfig);

                _clusterStatus.IsGeneratingAgents = false;
                _clusterStatus.AgentsGenerated = true;
                AddLog("Generazione agenti completata con successo");

                return Results.Ok(new { success = true, message = "Agenti generati con successo" });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Errore durante generazione agenti via API");
                _clusterStatus.IsGeneratingAgents = false;
                AddLog($"ERRORE generazione agenti: {ex.Message}");
                return Results.Problem(ex.Message);
            }
        });

        // Endpoint: POST /api/command/configure-communication
        app.MapPost("/api/command/configure-communication", async (HttpContext context) =>
        {
            Log.Information("API: POST /api/command/configure-communication");
            
            try
            {
                var configLoader = new ConfigurationLoader();
                var clusterConfig = configLoader.LoadClusterConfig("Config/cluster.yaml");

                _clusterStatus.IsConfiguringCommunication = true;
                _clusterStatus.LastCommand = "configure-communication";
                _clusterStatus.LastCommandTime = DateTime.UtcNow;

                await _communicationConfigurator.ConfigureCommunication(clusterConfig);

                _clusterStatus.IsConfiguringCommunication = false;
                _clusterStatus.CommunicationConfigured = true;
                AddLog("Configurazione comunicazione completata con successo");

                return Results.Ok(new { success = true, message = "Comunicazione configurata" });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Errore durante configurazione comunicazione via API");
                _clusterStatus.IsConfiguringCommunication = false;
                AddLog($"ERRORE configurazione comunicazione: {ex.Message}");
                return Results.Problem(ex.Message);
            }
        });

        // Endpoint: POST /api/command/validate
        app.MapPost("/api/command/validate", async (HttpContext context) =>
        {
            Log.Information("API: POST /api/command/validate");
            
            try
            {
                var configLoader = new ConfigurationLoader();
                var serversConfig = configLoader.LoadServersConfig("Config/servers.yaml");
                var clusterConfig = configLoader.LoadClusterConfig("Config/cluster.yaml");

                _clusterStatus.IsValidating = true;
                _clusterStatus.LastCommand = "validate";
                _clusterStatus.LastCommandTime = DateTime.UtcNow;

                var isValid = await _validationSuite.ValidateCluster(serversConfig, clusterConfig);

                _clusterStatus.IsValidating = false;
                _clusterStatus.ValidationPassed = isValid;
                AddLog(isValid ? "Validazione completata: OK" : "Validazione completata: ERRORI RILEVATI");

                return Results.Ok(new { success = isValid, message = isValid ? "Validazione OK" : "Validazione fallita" });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Errore durante validazione via API");
                _clusterStatus.IsValidating = false;
                AddLog($"ERRORE validazione: {ex.Message}");
                return Results.Problem(ex.Message);
            }
        });

        // Endpoint: POST /api/command/deploy
        app.MapPost("/api/command/deploy", async (HttpContext context) =>
        {
            Log.Information("API: POST /api/command/deploy (full workflow)");
            
            try
            {
                var configLoader = new ConfigurationLoader();
                var serversConfig = configLoader.LoadServersConfig("Config/servers.yaml");
                var clusterConfig = configLoader.LoadClusterConfig("Config/cluster.yaml");

                _clusterStatus.LastCommand = "deploy";
                _clusterStatus.LastCommandTime = DateTime.UtcNow;

                // Workflow completo
                AddLog("Inizio deploy completo...");

                _clusterStatus.IsProvisioning = true;
                await _provisioningEngine.ProvisionServers(serversConfig);
                _clusterStatus.IsProvisioning = false;
                _clusterStatus.ProvisioningCompleted = true;
                AddLog("Step 1/5: Provisioning completato");

                _clusterStatus.IsBuildingCluster = true;
                await _clusterBuilder.BuildCluster(clusterConfig);
                _clusterStatus.IsBuildingCluster = false;
                _clusterStatus.ClusterBuilt = true;
                AddLog("Step 2/5: Build cluster completato");

                _clusterStatus.IsGeneratingAgents = true;
                await _agentGenerator.GenerateAgents(clusterConfig);
                _clusterStatus.IsGeneratingAgents = false;
                _clusterStatus.AgentsGenerated = true;
                AddLog("Step 3/5: Generazione agenti completata");

                _clusterStatus.IsConfiguringCommunication = true;
                await _communicationConfigurator.ConfigureCommunication(clusterConfig);
                _clusterStatus.IsConfiguringCommunication = false;
                _clusterStatus.CommunicationConfigured = true;
                AddLog("Step 4/5: Configurazione comunicazione completata");

                _clusterStatus.IsValidating = true;
                var isValid = await _validationSuite.ValidateCluster(serversConfig, clusterConfig);
                _clusterStatus.IsValidating = false;
                _clusterStatus.ValidationPassed = isValid;
                AddLog($"Step 5/5: Validazione {(isValid ? "OK" : "FALLITA")}");

                AddLog("Deploy completo terminato");

                return Results.Ok(new { success = isValid, message = "Deploy completato" });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Errore durante deploy via API");
                AddLog($"ERRORE deploy: {ex.Message}");
                return Results.Problem(ex.Message);
            }
        });

        // Endpoint: GET /api/health
        app.MapGet("/api/health", () =>
        {
            return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        });

        // Endpoint: POST /api/agents/{name}/test (MODALITÀ SIMULATA)
        app.MapPost("/api/agents/{name}/test", async (string name, HttpContext context) =>
        {
            Log.Information($"API: POST /api/agents/{name}/test (modalità simulata)");
            
            try
            {
                var configLoader = new ConfigurationLoader();
                var clusterConfig = configLoader.LoadClusterConfig("Config/cluster.yaml");
                
                // Verifica che l'agente esista nella configurazione
                var agent = clusterConfig.Agents.FirstOrDefault(a => a.Name == name);
                if (agent == null)
                {
                    AddLog($"Test agente fallito: '{name}' non trovato nella configurazione");
                    return Results.NotFound(new 
                    { 
                        Success = false, 
                        Message = $"Agente '{name}' non trovato nella configurazione cluster" 
                    });
                }
                
                // Test simulato (gli agenti non sono processi reali)
                AddLog($"Test simulato per agente '{name}'");
                
                // Simula breve elaborazione
                await Task.Delay(300);
                
                return Results.Ok(new 
                { 
                    Success = true, 
                    Message = "Test simulato completato",
                    Details = "L'agente non è un processo reale, test simulato"
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Errore durante test agente {name}");
                AddLog($"ERRORE test agente '{name}': {ex.Message}");
                return Results.Problem(ex.Message);
            }
        });

        Log.Information("LocalApiServer in ascolto su http://localhost:5000");
        app.Run();
    }

    private void AddLog(string message)
    {
        var logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";
        _recentLogs.Add(logEntry);

        // Mantieni solo gli ultimi MaxRecentLogs
        if (_recentLogs.Count > MaxRecentLogs)
        {
            _recentLogs.RemoveAt(0);
        }

        Log.Information(message);
    }
}

/// <summary>
/// Stato corrente del cluster per API
/// </summary>
public class ClusterStatus
{
    public bool IsProvisioning { get; set; }
    public bool IsBuildingCluster { get; set; }
    public bool IsGeneratingAgents { get; set; }
    public bool IsConfiguringCommunication { get; set; }
    public bool IsValidating { get; set; }

    public bool ProvisioningCompleted { get; set; }
    public bool ClusterBuilt { get; set; }
    public bool AgentsGenerated { get; set; }
    public bool CommunicationConfigured { get; set; }
    public bool ValidationPassed { get; set; }

    public string LastCommand { get; set; } = "";
    public DateTime? LastCommandTime { get; set; }
}
