using Serilog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Bootstrapper.Modules;

namespace Bootstrapper;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Configurazione Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("Logs/bootstrapper-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("=== INDIGO BOOTSTRAPPER ===");
            Log.Information("Avvio applicazione...");

            // Carica configurazioni YAML
            var configLoader = new ConfigurationLoader();
            var serversConfig = configLoader.LoadServersConfig("Config/servers.yaml");
            var clusterConfig = configLoader.LoadClusterConfig("Config/cluster.yaml");

            Log.Information($"Configurazione caricata: {serversConfig.Servers.Count} server(i), Cluster: {clusterConfig.ClusterName}");

            // Inizializza moduli
            var remoteExecutor = new RemoteExecutor();
            var provisioningEngine = new ProvisioningEngine(remoteExecutor);
            var clusterBuilder = new ClusterBuilder();
            var agentGenerator = new AgentGenerator();
            var communicationConfigurator = new CommunicationConfigurator();
            var validationSuite = new ValidationSuite(remoteExecutor);

            // Avvia LocalApiServer in background
            var apiServer = new LocalApiServer(
                provisioningEngine,
                clusterBuilder,
                agentGenerator,
                communicationConfigurator,
                validationSuite
            );

            var apiServerTask = Task.Run(() => apiServer.Start());
            Log.Information("LocalApiServer avviato su http://localhost:5000");

            // Parsing comandi CLI
            if (args.Length == 0)
            {
                ShowHelp();
                return 0;
            }

            var command = args[0].ToLower();

            switch (command)
            {
                case "provision":
                    Log.Information("Comando: Provision");
                    await provisioningEngine.ProvisionServers(serversConfig);
                    break;

                case "build-cluster":
                    Log.Information("Comando: Build Cluster");
                    await clusterBuilder.BuildCluster(clusterConfig);
                    break;

                case "generate-agents":
                    Log.Information("Comando: Generate Agents");
                    await agentGenerator.GenerateAgents(clusterConfig);
                    break;

                case "configure-communication":
                    Log.Information("Comando: Configure Communication");
                    await communicationConfigurator.ConfigureCommunication(clusterConfig);
                    break;

                case "validate":
                    Log.Information("Comando: Validate");
                    var isValid = await validationSuite.ValidateCluster(serversConfig, clusterConfig);
                    return isValid ? 0 : 1;

                case "deploy":
                    Log.Information("Comando: Deploy (Full workflow)");
                    await ExecuteFullWorkflow(
                        provisioningEngine,
                        clusterBuilder,
                        agentGenerator,
                        communicationConfigurator,
                        validationSuite,
                        serversConfig,
                        clusterConfig
                    );
                    break;

                case "serve":
                    Log.Information("Modalità Server: In attesa di comandi via API...");
                    Console.WriteLine("Premi CTRL+C per terminare.");
                    await Task.Delay(Timeout.Infinite);
                    break;

                default:
                    Log.Warning($"Comando sconosciuto: {command}");
                    ShowHelp();
                    return 1;
            }

            Log.Information("Operazione completata con successo.");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Errore fatale durante l'esecuzione");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    static void ShowHelp()
    {
        Console.WriteLine(@"
INDIGO BOOTSTRAPPER - Comandi disponibili:

  provision               Esegue provisioning dei server (Docker, WSL2, firewall)
  build-cluster           Genera docker-compose.yaml, reti, volumi, Traefik
  generate-agents         Genera progetti agenti .NET 8 Minimal API da template
  configure-communication Configura Redis/RabbitMQ, code, topic, JWT
  validate                Valida configurazione e connettività (WinRM, API, heartbeat)
  deploy                  Esegue workflow completo (provision -> build -> generate -> configure -> validate)
  serve                   Avvia in modalità server per ricevere comandi via LocalApiServer API

Esempi:
  Bootstrapper.exe provision
  Bootstrapper.exe deploy
  Bootstrapper.exe serve
");
    }

    static async Task ExecuteFullWorkflow(
        ProvisioningEngine provisioningEngine,
        ClusterBuilder clusterBuilder,
        AgentGenerator agentGenerator,
        CommunicationConfigurator communicationConfigurator,
        ValidationSuite validationSuite,
        ServersConfig serversConfig,
        ClusterConfig clusterConfig)
    {
        Log.Information("=== WORKFLOW COMPLETO: DEPLOY ===");

        Log.Information("Step 1/5: Provisioning server...");
        await provisioningEngine.ProvisionServers(serversConfig);

        Log.Information("Step 2/5: Build cluster...");
        await clusterBuilder.BuildCluster(clusterConfig);

        Log.Information("Step 3/5: Generazione agenti...");
        await agentGenerator.GenerateAgents(clusterConfig);

        Log.Information("Step 4/5: Configurazione comunicazione...");
        await communicationConfigurator.ConfigureCommunication(clusterConfig);

        Log.Information("Step 5/5: Validazione finale...");
        var isValid = await validationSuite.ValidateCluster(serversConfig, clusterConfig);

        if (isValid)
        {
            Log.Information("=== DEPLOY COMPLETATO CON SUCCESSO ===");
        }
        else
        {
            Log.Error("=== DEPLOY FALLITO - ERRORI DI VALIDAZIONE ===");
            throw new Exception("Validazione cluster fallita");
        }
    }
}

// Classi per configurazione YAML
public class ConfigurationLoader
{
    private readonly IDeserializer _deserializer;

    public ConfigurationLoader()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    public ServersConfig LoadServersConfig(string path)
    {
        var yaml = File.ReadAllText(path);
        return _deserializer.Deserialize<ServersConfig>(yaml);
    }

    public ClusterConfig LoadClusterConfig(string path)
    {
        var yaml = File.ReadAllText(path);
        return _deserializer.Deserialize<ClusterConfig>(yaml);
    }
}

public class ServersConfig
{
    public List<ServerInfo> Servers { get; set; } = new();
}

public class ServerInfo
{
    public string Name { get; set; } = "";
    public string Hostname { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public bool InstallDocker { get; set; }
    public bool InstallWsl2 { get; set; }
}

public class ClusterConfig
{
    public string ClusterName { get; set; } = "";
    public List<AgentInfo> Agents { get; set; } = new();
    public NetworkConfig Network { get; set; } = new();
    public CommunicationConfig Communication { get; set; } = new();
}

public class AgentInfo
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public int Port { get; set; }
    public Dictionary<string, string> Environment { get; set; } = new();
}

public class NetworkConfig
{
    public string SubnetCidr { get; set; } = "";
    public bool UseTraefik { get; set; }
}

public class CommunicationConfig
{
    public string MessageBroker { get; set; } = "RabbitMQ";
    public string CacheProvider { get; set; } = "Redis";
    public bool UseJwt { get; set; }
}
