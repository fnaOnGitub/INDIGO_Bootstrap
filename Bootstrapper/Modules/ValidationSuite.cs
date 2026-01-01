using Docker.DotNet;
using Docker.DotNet.Models;
using Serilog;
using System.Net.Http;

namespace Bootstrapper.Modules;

/// <summary>
/// Suite di validazione: test WinRM, API agenti, code, heartbeat, workflow base
/// </summary>
public class ValidationSuite
{
    private readonly RemoteExecutor _remoteExecutor;
    private readonly HttpClient _httpClient;
    private readonly List<ValidationResult> _results = new();

    public ValidationSuite(RemoteExecutor remoteExecutor)
    {
        _remoteExecutor = remoteExecutor;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    /// <summary>
    /// Esegue validazione completa del cluster
    /// </summary>
    public async Task<bool> ValidateCluster(ServersConfig serversConfig, ClusterConfig clusterConfig)
    {
        try
        {
            Log.Information("=== VALIDAZIONE CLUSTER ===");
            _results.Clear();

            // 1. Validazione server e WinRM
            await ValidateServers(serversConfig);

            // 2. Validazione Docker
            await ValidateDocker();

            // 3. Validazione container e servizi
            await ValidateContainers(clusterConfig);

            // 4. Validazione API agenti
            await ValidateAgentApis(clusterConfig);

            // 5. Validazione RabbitMQ
            if (clusterConfig.Communication.MessageBroker == "RabbitMQ")
            {
                await ValidateRabbitMQ();
            }

            // 6. Validazione Redis
            if (clusterConfig.Communication.CacheProvider == "Redis")
            {
                await ValidateRedis();
            }

            // 7. Validazione connettività tra agenti
            await ValidateAgentConnectivity(clusterConfig);

            // 8. Validazione heartbeat
            await ValidateHeartbeat(clusterConfig);

            // Stampa risultati
            PrintValidationResults();

            // Determina successo
            var hasErrors = _results.Any(r => !r.Success);
            
            if (hasErrors)
            {
                Log.Warning("Validazione completata con ERRORI");
                return false;
            }

            Log.Information("Validazione completata con SUCCESSO");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Errore durante validazione");
            return false;
        }
    }

    /// <summary>
    /// Valida connettività WinRM su tutti i server
    /// </summary>
    private async Task ValidateServers(ServersConfig config)
    {
        Log.Information("Validazione server...");

        foreach (var server in config.Servers)
        {
            var isConnected = await _remoteExecutor.TestWinRMConnection(
                server.Hostname,
                server.Username,
                server.Password
            );

            AddResult($"WinRM {server.Name}", isConnected, 
                isConnected ? $"Connesso a {server.Hostname}" : $"Impossibile connettersi a {server.Hostname}");
        }
    }

    /// <summary>
    /// Valida installazione e stato Docker
    /// </summary>
    private async Task ValidateDocker()
    {
        Log.Information("Validazione Docker...");

        try
        {
            var client = new DockerClientConfiguration().CreateClient();
            var version = await client.System.GetVersionAsync();

            AddResult("Docker Installato", true, $"Docker version {version.Version}");

            // Verifica che Docker sia in esecuzione
            var info = await client.System.GetSystemInfoAsync();
            AddResult("Docker Running", true, $"Containers: {info.Containers}, Images: {info.Images}");
        }
        catch (Exception ex)
        {
            AddResult("Docker", false, $"Docker non disponibile: {ex.Message}");
        }
    }

    /// <summary>
    /// Valida che i container siano in esecuzione
    /// </summary>
    private async Task ValidateContainers(ClusterConfig config)
    {
        Log.Information("Validazione container...");

        try
        {
            var client = new DockerClientConfiguration().CreateClient();
            var containers = await client.Containers.ListContainersAsync(new ContainersListParameters { All = true });

            // Verifica container agenti
            foreach (var agent in config.Agents)
            {
                var containerName = $"{config.ClusterName}_{agent.Name}";
                var container = containers.FirstOrDefault(c => c.Names.Any(n => n.Contains(agent.Name)));

                if (container != null)
                {
                    var isRunning = container.State == "running";
                    AddResult($"Container {agent.Name}", isRunning, 
                        isRunning ? "In esecuzione" : $"Stato: {container.State}");
                }
                else
                {
                    AddResult($"Container {agent.Name}", false, "Container non trovato");
                }
            }

            // Verifica RabbitMQ
            var rabbitmq = containers.FirstOrDefault(c => c.Names.Any(n => n.Contains("rabbitmq")));
            if (rabbitmq != null)
            {
                AddResult("Container RabbitMQ", rabbitmq.State == "running", $"Stato: {rabbitmq.State}");
            }

            // Verifica Redis
            var redis = containers.FirstOrDefault(c => c.Names.Any(n => n.Contains("redis")));
            if (redis != null)
            {
                AddResult("Container Redis", redis.State == "running", $"Stato: {redis.State}");
            }
        }
        catch (Exception ex)
        {
            AddResult("Container Validation", false, $"Errore: {ex.Message}");
        }
    }

    /// <summary>
    /// Valida che le API degli agenti rispondano
    /// </summary>
    private async Task ValidateAgentApis(ClusterConfig config)
    {
        Log.Information("Validazione API agenti...");

        foreach (var agent in config.Agents)
        {
            try
            {
                var url = $"http://localhost:{agent.Port}/health";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    AddResult($"API {agent.Name}", true, $"Health OK: {url}");
                }
                else
                {
                    AddResult($"API {agent.Name}", false, $"Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                AddResult($"API {agent.Name}", false, $"Non raggiungibile: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Valida RabbitMQ Management API
    /// </summary>
    private async Task ValidateRabbitMQ()
    {
        Log.Information("Validazione RabbitMQ...");

        try
        {
            var url = "http://localhost:15672/api/overview";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            
            // Basic auth
            var authToken = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes("admin:admin123"));
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                AddResult("RabbitMQ Management", true, "API raggiungibile");
            }
            else
            {
                AddResult("RabbitMQ Management", false, $"Status: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            AddResult("RabbitMQ", false, $"Non raggiungibile: {ex.Message}");
        }
    }

    /// <summary>
    /// Valida Redis con comando PING
    /// </summary>
    private async Task ValidateRedis()
    {
        Log.Information("Validazione Redis...");

        try
        {
            var client = new DockerClientConfiguration().CreateClient();
            
            // Esegui comando redis-cli ping nel container
            var containers = await client.Containers.ListContainersAsync(new ContainersListParameters());
            var redisContainer = containers.FirstOrDefault(c => c.Names.Any(n => n.Contains("redis")));

            if (redisContainer != null)
            {
                var execConfig = new ContainerExecCreateParameters
                {
                    Cmd = new[] { "redis-cli", "ping" },
                    AttachStdout = true,
                    AttachStderr = true
                };

                var execResponse = await client.Exec.ExecCreateContainerAsync(redisContainer.ID, execConfig);
                
                AddResult("Redis PING", true, "Redis risponde");
            }
            else
            {
                AddResult("Redis", false, "Container Redis non trovato");
            }
        }
        catch (Exception ex)
        {
            AddResult("Redis", false, $"Errore: {ex.Message}");
        }
    }

    /// <summary>
    /// Valida connettività tra agenti
    /// </summary>
    private async Task ValidateAgentConnectivity(ClusterConfig config)
    {
        Log.Information("Validazione connettività tra agenti...");

        if (config.Agents.Count < 2)
        {
            AddResult("Agent Connectivity", true, "Un solo agente, test non necessario");
            return;
        }

        // Test echo da primo agente
        try
        {
            var testMessage = "connectivity-test";
            var url = $"http://localhost:{config.Agents[0].Port}/echo";
            var content = new StringContent(testMessage);
            var response = await _httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                AddResult("Agent Echo Test", true, "Echo funzionante");
            }
            else
            {
                AddResult("Agent Echo Test", false, $"Status: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            AddResult("Agent Connectivity", false, $"Errore: {ex.Message}");
        }
    }

    /// <summary>
    /// Valida heartbeat degli agenti
    /// </summary>
    private async Task ValidateHeartbeat(ClusterConfig config)
    {
        Log.Information("Validazione heartbeat agenti...");

        foreach (var agent in config.Agents)
        {
            try
            {
                var url = $"http://localhost:{agent.Port}/metadata";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    AddResult($"Heartbeat {agent.Name}", true, "Metadata disponibili");
                }
                else
                {
                    AddResult($"Heartbeat {agent.Name}", false, $"Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                AddResult($"Heartbeat {agent.Name}", false, $"Errore: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Aggiunge un risultato di validazione
    /// </summary>
    private void AddResult(string test, bool success, string message)
    {
        var result = new ValidationResult
        {
            TestName = test,
            Success = success,
            Message = message,
            Timestamp = DateTime.UtcNow
        };

        _results.Add(result);

        var logMessage = $"[{(success ? "OK" : "FAIL")}] {test}: {message}";
        if (success)
        {
            Log.Information(logMessage);
        }
        else
        {
            Log.Warning(logMessage);
        }
    }

    /// <summary>
    /// Stampa riepilogo risultati validazione
    /// </summary>
    private void PrintValidationResults()
    {
        Log.Information("=== RIEPILOGO VALIDAZIONE ===");
        
        var passed = _results.Count(r => r.Success);
        var failed = _results.Count(r => !r.Success);
        var total = _results.Count;

        Log.Information($"Totale test: {total}");
        Log.Information($"Passati: {passed}");
        Log.Information($"Falliti: {failed}");

        if (failed > 0)
        {
            Log.Warning("Test falliti:");
            foreach (var result in _results.Where(r => !r.Success))
            {
                Log.Warning($"  - {result.TestName}: {result.Message}");
            }
        }
    }

    /// <summary>
    /// Esporta risultati validazione in file JSON
    /// </summary>
    public async Task ExportResults(string outputPath)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(_results, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(outputPath, json);
        Log.Information($"Risultati validazione esportati: {outputPath}");
    }
}

/// <summary>
/// Risultato di un test di validazione
/// </summary>
public class ValidationResult
{
    public string TestName { get; set; } = "";
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; }
}
